using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Android;

public class PosItem
{
	public Guid CatalogItemId { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Price { get; set; }
	public decimal Quantity { get; set; } = 1;
	public string TVA { get; set; } = string.Empty;
	public decimal Total => Price * Quantity;
}

public partial class MainPage : ContentPage
{
	private const string ApiBaseUrl = "https://localhost:7173/api/items";
	private List<CatalogItemDTO> _catalogItems = new();
	private List<PosItem> _currentSale = new();
	private readonly string _localStoragePath;

	public MainPage()
	{
		InitializeComponent();
		_localStoragePath = Path.Combine(FileSystem.AppDataDirectory, "catalogitems.json");
		LoadCatalogItems();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadCatalogItems();
	}

	private async void OnRefreshItemsClicked(object sender, EventArgs e)
	{
		await LoadCatalogItems();
	}

	private async Task LoadCatalogItems()
	{
		try
		{
			StatusLabel.Text = "Loading items...";

			// Try API first
			var itemsFromApi = await FetchItemsFromApiAsync();
			if (itemsFromApi != null && itemsFromApi.Count > 0)
			{
				_catalogItems = itemsFromApi;
				await SaveItemsLocallyAsync(_catalogItems);
				StatusLabel.Text = "Ready";
			}
			else
			{
				// Fallback to local
				_catalogItems = await LoadItemsFromLocalAsync();
				StatusLabel.Text = _catalogItems.Count > 0 ? "Ready (Offline)" : "No items available";
			}

			CatalogItemsCollectionView.ItemsSource = null;
			CatalogItemsCollectionView.ItemsSource = _catalogItems;
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "Error loading items";
			Debug.WriteLine($"Error: {ex.Message}");
		}
	}

	private void OnAddToPosClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is CatalogItemDTO item)
		{
			var existingItem = _currentSale.FirstOrDefault(i => i.CatalogItemId == item.Id);
			if (existingItem != null)
			{
				existingItem.Quantity++;
			}
			else
			{
				_currentSale.Add(new PosItem
				{
					CatalogItemId = item.Id,
					Name = item.Name,
					Price = item.Price,
					Quantity = 1,
					TVA = item.TVA
				});
			}

			RefreshCurrentSale();
		}
	}

	private void OnRemoveFromPosClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is PosItem item)
		{
			_currentSale.Remove(item);
			RefreshCurrentSale();
		}
	}

	private void RefreshCurrentSale()
	{
		CurrentSaleCollectionView.ItemsSource = null;
		CurrentSaleCollectionView.ItemsSource = _currentSale;
		UpdateTotals();
	}

	private void UpdateTotals()
	{
		decimal subtotal = _currentSale.Sum(i => i.Total);
		SubtotalLabel.Text = $"Subtotal: ${subtotal:F2}";
		TotalLabel.Text = $"TOTAL: ${subtotal:F2}";
	}

	private void OnClearSaleClicked(object sender, EventArgs e)
	{
		_currentSale.Clear();
		RefreshCurrentSale();
	}

	private async void OnPayCashClicked(object sender, EventArgs e)
	{
		await ProcessPayment("Cash");
	}

	private async void OnPayCardClicked(object sender, EventArgs e)
	{
		await ProcessPayment("Card");
	}

	private async Task ProcessPayment(string paymentMethod)
	{
		if (_currentSale.Count == 0)
		{
			await DisplayAlert("No Items", "Please add items to the sale first", "OK");
			return;
		}

		try
		{
			StatusLabel.Text = $"Processing {paymentMethod} payment...";

			decimal total = _currentSale.Sum(i => i.Total);

			// Generate bill/receipt
			string receipt = GenerateReceipt(paymentMethod, total);

			// Create bill DTO for API
			var billDto = new BillDTO
			{
				Id = Guid.NewGuid(),
				BillDate = DateTime.UtcNow,
				PaymentMethod = paymentMethod,
				Subtotal = total,
				Total = total,
				ReceiptContent = receipt,
				Items = _currentSale.Select(item => new BillItemDTO
				{
					Id = Guid.NewGuid(),
					Name = item.Name,
					Price = item.Price,
					Quantity = item.Quantity,
					TVA = item.TVA,
					Total = item.Total
				}).ToList()
			};

			// Send bill to API
			bool apiSuccess = await SendBillToApiAsync(billDto);

			// Save receipt locally regardless of API success
			string fileName = $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
			string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
			await File.WriteAllTextAsync(filePath, receipt);

			StatusLabel.Text = apiSuccess ? "Payment completed & synced!" : "Payment completed (saved locally)";

			// Show receipt
			bool viewReceipt = await DisplayAlert("Payment Successful", 
				$"Payment of ${total:F2} completed via {paymentMethod}\n\n" +
				(apiSuccess ? "✓ Synced to server\n" : "⚠ Saved locally only\n") +
				$"\nReceipt saved to:\n{filePath}", 
				"View Receipt", "Close");

			if (viewReceipt)
			{
				await DisplayAlert("Receipt", receipt, "Close");
			}

			// Clear the sale
			_currentSale.Clear();
			RefreshCurrentSale();
			StatusLabel.Text = "Ready";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "Payment failed";
			await DisplayAlert("Error", $"Payment processing failed: {ex.Message}", "OK");
			Debug.WriteLine($"Payment error: {ex}");
		}
	}

	private async Task<bool> SendBillToApiAsync(BillDTO bill)
	{
		try
		{
			using (var client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromSeconds(10);
				var json = JsonSerializer.Serialize(bill, new JsonSerializerOptions { WriteIndented = true });
				var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

				Debug.WriteLine($"Sending bill to API: {json}");

				var response = await client.PostAsync("https://localhost:7173/api/bills", content);

				if (response.IsSuccessStatusCode)
				{
					Debug.WriteLine("Bill sent to API successfully");
					return true;
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					Debug.WriteLine($"API Error: {errorContent}");
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error sending bill to API: {ex.Message}");
			return false;
		}
	}

	private string GenerateReceipt(string paymentMethod, decimal total)
	{
		var sb = new StringBuilder();
		sb.AppendLine("========================================");
		sb.AppendLine("           ORDERCLOUD RECEIPT           ");
		sb.AppendLine("========================================");
		sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		sb.AppendLine($"Payment Method: {paymentMethod}");
		sb.AppendLine("========================================");
		sb.AppendLine();
		sb.AppendLine("ITEMS:");
		sb.AppendLine("----------------------------------------");

		foreach (var item in _currentSale)
		{
			sb.AppendLine($"{item.Name}");
			sb.AppendLine($"  {item.Quantity} x ${item.Price:F2} = ${item.Total:F2}");
			if (!string.IsNullOrWhiteSpace(item.TVA))
			{
				sb.AppendLine($"  TVA: {item.TVA}");
			}
			sb.AppendLine();
		}

		sb.AppendLine("========================================");
		sb.AppendLine($"SUBTOTAL:                    ${total:F2}");
		sb.AppendLine($"TOTAL:                       ${total:F2}");
		sb.AppendLine("========================================");
		sb.AppendLine();
		sb.AppendLine("      Thank you for your business!     ");
		sb.AppendLine("========================================");

		return sb.ToString();
	}

	private async Task<List<CatalogItemDTO>?> FetchItemsFromApiAsync()
	{
		try
		{
			using (var client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromSeconds(10);
				var response = await client.GetAsync(ApiBaseUrl);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				return JsonSerializer.Deserialize<List<CatalogItemDTO>>(json, options);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"API Error: {ex.Message}");
			return null;
		}
	}

	private async Task SaveItemsLocallyAsync(List<CatalogItemDTO> items)
	{
		try
		{
			var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(_localStoragePath, json);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error saving: {ex.Message}");
		}
	}

	private async Task<List<CatalogItemDTO>> LoadItemsFromLocalAsync()
	{
		try
		{
			if (File.Exists(_localStoragePath))
			{
				var json = await File.ReadAllTextAsync(_localStoragePath);
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var items = JsonSerializer.Deserialize<List<CatalogItemDTO>>(json, options);
				return items ?? new List<CatalogItemDTO>();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error loading: {ex.Message}");
		}

		return new List<CatalogItemDTO>();
	}
}

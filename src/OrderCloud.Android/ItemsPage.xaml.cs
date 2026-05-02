using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Android;

public class SaleItem
{
	public Guid CatalogItemId { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Price { get; set; }
	public int Quantity { get; set; } = 1;
	public string TVA { get; set; } = string.Empty;
	public decimal Total => Price * Quantity;
}

public partial class ItemsPage : ContentPage, IQueryAttributable
{
	private const string ItemsApiUrl = "https://localhost:7173/api/items";
	private const string BillsApiUrl = "https://localhost:7173/api/bills";

	private List<CatalogItemDTO> _catalogItems = new();
	private List<CatalogItemDTO> _filteredItems = new();
	private List<SaleItem> _cart = new();

	public ItemsPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadCatalogItems();
	}

	// --- Catalog Loading ---

	private async Task LoadCatalogItems()
	{
		try
		{
			StatusLabel.Text = "Loading items...";

			var itemsFromApi = await FetchItemsFromApiAsync();
			if (itemsFromApi != null && itemsFromApi.Count > 0)
			{
				_catalogItems = itemsFromApi;
				_filteredItems = new List<CatalogItemDTO>(_catalogItems);
				StatusLabel.Text = $"{_catalogItems.Count} items loaded";
			}
			else
			{
				_catalogItems = new List<CatalogItemDTO>();
				_filteredItems = new List<CatalogItemDTO>();
				StatusLabel.Text = "No items available";
			}

			Device.BeginInvokeOnMainThread(() =>
			{
				CatalogItemsCollectionView.ItemsSource = null;
				CatalogItemsCollectionView.ItemsSource = _filteredItems;
			});
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "Error loading items";
			Debug.WriteLine($"[LoadCatalogItems] Error: {ex.Message}");
		}
	}

	private async void OnRefreshClicked(object sender, EventArgs e)
	{
		await LoadCatalogItems();
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		var query = e.NewTextValue?.Trim().ToLower();

		if (string.IsNullOrEmpty(query))
		{
			_filteredItems = new List<CatalogItemDTO>(_catalogItems);
		}
		else
		{
			_filteredItems = _catalogItems
				.Where(i => i.Name.ToLower().Contains(query))
				.ToList();
		}

		CatalogItemsCollectionView.ItemsSource = null;
		CatalogItemsCollectionView.ItemsSource = _filteredItems;
	}

	// --- Cart Operations ---

	private void OnAddToCartClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is CatalogItemDTO item)
		{
			var existingItem = _cart.FirstOrDefault(i => i.CatalogItemId == item.Id);
			if (existingItem != null)
			{
				existingItem.Quantity++;
			}
			else
			{
				_cart.Add(new SaleItem
				{
					CatalogItemId = item.Id,
					Name = item.Name,
					Price = item.Price,
					Quantity = 1,
					TVA = item.TVA
				});
			}

			RefreshCart();
		}
	}

	private void OnIncreaseQuantityClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is SaleItem item)
		{
			item.Quantity++;
			RefreshCart();
		}
	}

	private void OnDecreaseQuantityClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is SaleItem item)
		{
			item.Quantity--;
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
			}
			RefreshCart();
		}
	}

	private void OnClearCartClicked(object sender, EventArgs e)
	{
		_cart.Clear();
		RefreshCart();
	}

	private void RefreshCart()
	{
		CartCollectionView.ItemsSource = null;
		CartCollectionView.ItemsSource = _cart;
		UpdateTotal();
	}

	private void UpdateTotal()
	{
		decimal total = _cart.Sum(i => i.Total);
		TotalLabel.Text = $"${total:F2}";
	}

	// --- Payment ---

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
		if (_cart.Count == 0)
		{
			await DisplayAlert("Empty Cart", "Add items to the cart first", "OK");
			return;
		}

		try
		{
			StatusLabel.Text = $"Processing {paymentMethod}...";

			decimal total = _cart.Sum(i => i.Total);
			string receipt = GenerateReceipt(paymentMethod, total);

			var billDto = new BillDTO
			{
				Id = Guid.NewGuid(),
				BillDate = DateTime.UtcNow,
				PaymentMethod = paymentMethod,
				Subtotal = total,
				Total = total,
				ReceiptContent = receipt,
				Items = _cart.Select(item => new BillItemDTO
				{
					Id = Guid.NewGuid(),
					Name = item.Name,
					Price = item.Price,
					Quantity = item.Quantity,
					TVA = item.TVA,
					Total = item.Total
				}).ToList()
			};

			bool billSent = await SendBillToApiAsync(billDto);

			string fileName = $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
			string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
			await File.WriteAllTextAsync(filePath, receipt);

			StatusLabel.Text = "Payment completed";

			await DisplayAlert("Payment Successful",
				$"Paid via {paymentMethod}\n" +
				$"Amount: ${total:F2}\n\n" +
				$"Bill: {(billSent ? "✓ Synced" : "⚠ Saved locally")}\n\n" +
				$"Receipt saved",
				"OK");

			_cart.Clear();
			RefreshCart();
			StatusLabel.Text = "Select items to sell";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "Payment failed";
			await DisplayAlert("Error", $"Payment failed: {ex.Message}", "OK");
			Debug.WriteLine($"Payment error: {ex}");
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

		foreach (var item in _cart)
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

	private async Task<bool> SendBillToApiAsync(BillDTO bill)
	{
		try
		{
			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(10);
			var json = JsonSerializer.Serialize(bill, new JsonSerializerOptions { WriteIndented = true });
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(BillsApiUrl, content);
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	private async Task<List<CatalogItemDTO>?> FetchItemsFromApiAsync()
	{
		try
		{
			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(10);
			Debug.WriteLine($"[FetchItems] GET {ItemsApiUrl}");

			var response = await client.GetAsync(ItemsApiUrl);
			Debug.WriteLine($"[FetchItems] Status: {response.StatusCode}");

			if (!response.IsSuccessStatusCode)
			{
				Debug.WriteLine($"[FetchItems] Error: {await response.Content.ReadAsStringAsync()}");
				return null;
			}

			var json = await response.Content.ReadAsStringAsync();
			Debug.WriteLine($"[FetchItems] JSON length: {json.Length}");

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var result = JsonSerializer.Deserialize<List<CatalogItemDTO>>(json, options);
			Debug.WriteLine($"[FetchItems] Deserialized: {result?.Count ?? 0} items");

			return result;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FetchItems] Exception: {ex.Message}");
			return null;
		}
	}

	// --- Old CRUD methods (kept for Add/Edit via ItemDetailPage if needed) ---

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("ReturnedItem", out var itemObj) && itemObj is CatalogItemDTO returnedItem)
		{
			if (query.TryGetValue("Action", out var actionObj) && actionObj is string action)
			{
				if (action == "ItemCreated")
				{
					_catalogItems.Add(returnedItem);
				}
				else if (action == "ItemUpdated")
				{
					var existing = _catalogItems.FirstOrDefault(i => i.Id == returnedItem.Id);
					if (existing != null)
					{
						var index = _catalogItems.IndexOf(existing);
						_catalogItems[index] = returnedItem;
					}
				}

				_filteredItems = new List<CatalogItemDTO>(_catalogItems);
				CatalogItemsCollectionView.ItemsSource = null;
				CatalogItemsCollectionView.ItemsSource = _filteredItems;
			}
		}

		// Очищаем параметры после обработки
		query.Clear();
	}

	private async void OnAddItemClicked(object sender, EventArgs e)
	{
		// Переход на страницу добавления с использованием Shell
		await Shell.Current.GoToAsync(nameof(ItemDetailPage));
	}

	private async void OnEditItemClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is CatalogItemDTO item)
		{
			var navigationParameter = new Dictionary<string, object> { { "Item", item } };
			// Вы можете добавить обработку "Item" внутри ItemDetailPage через IQueryAttributable
			await Shell.Current.GoToAsync(nameof(ItemDetailPage), navigationParameter);
		}
	}

	private async void OnDeleteItemClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is CatalogItemDTO item)
		{
			bool confirm = await DisplayAlert("Confirm Delete",
				$"Are you sure you want to delete '{item.Name}'?",
				"Delete", "Cancel");

			if (confirm)
			{
				try
				{
					using var client = new HttpClient();
					client.Timeout = TimeSpan.FromSeconds(10);
					var response = await client.DeleteAsync($"{ItemsApiUrl}/{item.Id}");

					if (response.IsSuccessStatusCode)
					{
						_catalogItems.Remove(item);
						_filteredItems.Remove(item);
						CatalogItemsCollectionView.ItemsSource = null;
						CatalogItemsCollectionView.ItemsSource = _filteredItems;
						StatusLabel.Text = "Item deleted";
					}
				}
				catch (Exception ex)
				{
					await DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
				}
			}
		}
	}
}

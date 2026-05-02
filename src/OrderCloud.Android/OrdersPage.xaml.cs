using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Android;

public partial class OrdersPage : ContentPage
{
    private string ApiBaseUrl => $"{Constants.ApiBaseUrl}/api/orders";
    private string BillsApiUrl => $"{Constants.ApiBaseUrl}/api/bills";
    
    private List<OrderDTO> _orders = new();
    private OrderDTO? _selectedOrder;

    public OrdersPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOrdersAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Refreshing from API...";
        await LoadOrdersAsync();
    }

    private void OnOrderSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is OrderDTO order)
        {
            ShowOrderDetail(order);
        }
    }

    private void OnBackToOrdersClicked(object sender, EventArgs e)
    {
        HideOrderDetail();
    }

    private void ShowOrderDetail(OrderDTO order)
    {
        _selectedOrder = order;

        OrdersCollectionView.IsVisible = false;
        OrderDetailCard.IsVisible = true;
        PaymentActions.IsVisible = true;

        var shortId = order.Id.ToString("N")[..8];
        OrderTitle.Text = $"Order #{shortId}";
        OrderDate.Text = order.CreatedAt.ToString("MMM dd, yyyy HH:mm");
        OrderStatus.Text = order.Status;
        OrderStatus.BackgroundColor = GetStatusColor(order.Status);

        OrderItemsCollectionView.ItemsSource = order.Items;
    }

    private void HideOrderDetail()
    {
        _selectedOrder = null;

        OrdersCollectionView.IsVisible = true;
        OrderDetailCard.IsVisible = false;
        PaymentActions.IsVisible = false;

        OrdersCollectionView.SelectedItem = null;
    }

    private Color GetStatusColor(string status)
    {
        return status?.ToLower() switch
        {
            "completed" => Color.Parse("#2ECC71"),
            "pending" => Color.Parse("#F39C12"),
            "cancelled" => Color.Parse("#E74C3C"),
            "processing" => Color.Parse("#3498DB"),
            "paid" => Color.Parse("#27AE60"),
            _ => Color.Parse("#8C8C8C")
        };
    }

    private async void OnPayCashClicked(object sender, EventArgs e)
    {
        if (_selectedOrder != null)
            await ProcessPayment(_selectedOrder, "Cash");
    }

    private async void OnPayCardClicked(object sender, EventArgs e)
    {
        if (_selectedOrder != null)
            await ProcessPayment(_selectedOrder, "Card");
    }

    private async Task ProcessPayment(OrderDTO order, string paymentMethod)
    {
        if (order.Items == null || order.Items.Count == 0)
        {
            await DisplayAlert("No Items", "This order has no items", "OK");
            return;
        }

        try
        {
            StatusLabel.Text = $"Processing {paymentMethod}...";

            var receipt = GenerateReceipt(order, paymentMethod);

            var billDto = new BillDTO
            {
                Id = Guid.NewGuid(),
                BillDate = DateTime.UtcNow,
                PaymentMethod = paymentMethod,
                Subtotal = order.Total,
                Total = order.Total,
                ReceiptContent = receipt,
                Items = order.Items.Select(item => new BillItemDTO
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
            bool orderPaid = await MarkOrderAsPaidAsync(order.Id);

            string fileName = $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            await File.WriteAllTextAsync(filePath, receipt);

            var shortId = order.Id.ToString("N")[..8];
            await DisplayAlert("Payment Successful",
                $"Order #{shortId}\n" +
                $"Paid via {paymentMethod}\n" +
                $"Amount: ${order.Total:F2}\n\n" +
                $"Bill: {(billSent ? "✓ Synced" : "⚠ Saved locally")}\n" +
                $"Order: {(orderPaid ? "✓ Updated" : "⚠ Saved locally")}\n\n" +
                $"Receipt saved",
                "OK");

            StatusLabel.Text = "Payment completed";
            HideOrderDetail();
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Payment failed";
            await DisplayAlert("Error", $"Payment failed: {ex.Message}", "OK");
            Debug.WriteLine($"Payment error: {ex}");
        }
    }

    private string GenerateReceipt(OrderDTO order, string paymentMethod)
    {
        var sb = new StringBuilder();
        var shortId = order.Id.ToString("N")[..8];
        sb.AppendLine("========================================");
        sb.AppendLine("           ORDERCLOUD RECEIPT           ");
        sb.AppendLine("========================================");
        sb.AppendLine($"Order #: {shortId}");
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Payment Method: {paymentMethod}");
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine("ITEMS:");
        sb.AppendLine("----------------------------------------");

        foreach (var item in order.Items)
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
        sb.AppendLine($"SUBTOTAL:                    ${order.Total:F2}");
        sb.AppendLine($"TOTAL:                       ${order.Total:F2}");
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

    private async Task<bool> MarkOrderAsPaidAsync(Guid orderId)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{ApiBaseUrl}/{orderId}");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var order = JsonSerializer.Deserialize<OrderDTO>(json, options);

            if (order == null) return false;

            order.Status = "Paid";

            var updateJson = JsonSerializer.Serialize(order);
            var content = new StringContent(updateJson, Encoding.UTF8, "application/json");

            var putResponse = await client.PutAsync($"{ApiBaseUrl}/{orderId}", content);
            return putResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            StatusLabel.Text = "Loading orders...";

            var ordersFromApi = await FetchOrdersFromApiAsync();
            if (ordersFromApi != null && ordersFromApi.Count > 0)
            {
                _orders = ordersFromApi;
                StatusLabel.Text = $"Loaded {_orders.Count} orders";
            }
            else
            {
                _orders = new List<OrderDTO>();
                StatusLabel.Text = "No orders found";
            }

            RefreshOrdersList();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            Debug.WriteLine($"Error loading orders: {ex.Message}");
        }
    }

    private void RefreshOrdersList()
    {
        OrdersCollectionView.ItemsSource = null;
        OrdersCollectionView.ItemsSource = _orders;
    }

    private async Task<List<OrderDTO>?> FetchOrdersFromApiAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetAsync(ApiBaseUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<OrderDTO>>(json, options);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error: {ex.Message}");
            return null;
        }
    }
}


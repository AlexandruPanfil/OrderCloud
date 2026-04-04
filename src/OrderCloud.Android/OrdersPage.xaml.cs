using System.Diagnostics;
using System.Text.Json;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Android;

public partial class OrdersPage : ContentPage
{
    private readonly string _localStoragePath;
    private const string OrdersFileName = "orders.json";
    private const string ApiBaseUrl = "https://localhost:7173/api/orders";
    private List<OrderDTO> _orders = new();

    public OrdersPage()
    {
        InitializeComponent();
        _localStoragePath = Path.Combine(FileSystem.AppDataDirectory, OrdersFileName);
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

    private async Task LoadOrdersAsync()
    {
        try
        {
            StatusLabel.Text = "Loading orders...";
            
            // Try to load from API first
            var ordersFromApi = await FetchOrdersFromApiAsync();
            if (ordersFromApi != null && ordersFromApi.Count > 0)
            {
                _orders = ordersFromApi;
                await SaveOrdersLocallyAsync(_orders);
                StatusLabel.Text = $"Loaded {_orders.Count} orders from API";
            }
            else
            {
                // Fall back to local storage
                _orders = await LoadOrdersFromLocalAsync();
                if (_orders.Count > 0)
                {
                    StatusLabel.Text = $"Loaded {_orders.Count} orders from local storage (offline)";
                }
                else
                {
                    StatusLabel.Text = "No orders found.";
                }
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
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetAsync(ApiBaseUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                var orders = JsonSerializer.Deserialize<List<OrderDTO>>(json, options);
                return orders;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error: {ex.Message}");
            return null;
        }
    }

    private async Task SaveOrdersLocallyAsync(List<OrderDTO> orders)
    {
        try
        {
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_localStoragePath, json);
            Debug.WriteLine($"Orders saved locally to: {_localStoragePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving orders locally: {ex.Message}");
        }
    }

    private async Task<List<OrderDTO>> LoadOrdersFromLocalAsync()
    {
        try
        {
            if (File.Exists(_localStoragePath))
            {
                var json = await File.ReadAllTextAsync(_localStoragePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = JsonSerializer.Deserialize<List<OrderDTO>>(json, options);
                return orders ?? new List<OrderDTO>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading orders locally: {ex.Message}");
        }

        return new List<OrderDTO>();
    }
}

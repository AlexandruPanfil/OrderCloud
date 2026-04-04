using System.Diagnostics;
using System.Text.Json;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Android;

public partial class ItemsPage : ContentPage
{
    private readonly string _localStoragePath;
    private const string ItemsFileName = "catalogitems.json";
    private const string ApiBaseUrl = "https://localhost:7173/api/items";
    private List<CatalogItemDTO> _items = new();

    public ItemsPage()
    {
        InitializeComponent();
        _localStoragePath = Path.Combine(FileSystem.AppDataDirectory, ItemsFileName);
        
        // Subscribe to messaging center events
        MessagingCenter.Subscribe<ItemDetailPage, CatalogItemDTO>(this, "ItemCreated", async (sender, item) =>
        {
            await CreateItemAsync(item);
        });
        
        MessagingCenter.Subscribe<ItemDetailPage, CatalogItemDTO>(this, "ItemUpdated", async (sender, item) =>
        {
            await UpdateItemAsync(item);
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadItemsAsync();
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ItemDetailPage());
    }

    private async void OnEditItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is CatalogItemDTO item)
        {
            await Navigation.PushAsync(new ItemDetailPage(item));
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
                await DeleteItemAsync(item);
            }
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Refreshing from API...";
        await LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            StatusLabel.Text = "Loading items...";
            
            // Try to load from API first
            var itemsFromApi = await FetchItemsFromApiAsync();
            if (itemsFromApi != null && itemsFromApi.Count > 0)
            {
                _items = itemsFromApi;
                await SaveItemsLocallyAsync(_items);
                StatusLabel.Text = $"Loaded {_items.Count} items from API";
            }
            else
            {
                // Fall back to local storage
                _items = await LoadItemsFromLocalAsync();
                if (_items.Count > 0)
                {
                    StatusLabel.Text = $"Loaded {_items.Count} items from local storage (offline)";
                }
                else
                {
                    StatusLabel.Text = "No items found. Add your first item!";
                }
            }

            RefreshItemsList();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            Debug.WriteLine($"Error loading items: {ex.Message}");
        }
    }

    private async Task CreateItemAsync(CatalogItemDTO item)
    {
        try
        {
            StatusLabel.Text = "Creating item...";
            
            using (var client = new HttpClient())
            {
                var json = JsonSerializer.Serialize(item);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync(ApiBaseUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var createdItem = JsonSerializer.Deserialize<CatalogItemDTO>(responseJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (createdItem != null)
                    {
                        _items.Add(createdItem);
                        await SaveItemsLocallyAsync(_items);
                        RefreshItemsList();
                        StatusLabel.Text = $"Item '{item.Name}' created successfully!";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    StatusLabel.Text = $"Error creating item: {errorContent}";
                    await DisplayAlert("Error", $"Failed to create item: {errorContent}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            await DisplayAlert("Error", $"Failed to create item: {ex.Message}", "OK");
            Debug.WriteLine($"Error creating item: {ex.Message}");
        }
    }

    private async Task UpdateItemAsync(CatalogItemDTO item)
    {
        try
        {
            StatusLabel.Text = "Updating item...";
            
            using (var client = new HttpClient())
            {
                var json = JsonSerializer.Serialize(item);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.PutAsync($"{ApiBaseUrl}/{item.Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var updatedItem = JsonSerializer.Deserialize<CatalogItemDTO>(responseJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (updatedItem != null)
                    {
                        var index = _items.FindIndex(i => i.Id == item.Id);
                        if (index >= 0)
                        {
                            _items[index] = updatedItem;
                            await SaveItemsLocallyAsync(_items);
                            RefreshItemsList();
                            StatusLabel.Text = $"Item '{item.Name}' updated successfully!";
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    StatusLabel.Text = $"Error updating item: {errorContent}";
                    await DisplayAlert("Error", $"Failed to update item: {errorContent}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            await DisplayAlert("Error", $"Failed to update item: {ex.Message}", "OK");
            Debug.WriteLine($"Error updating item: {ex.Message}");
        }
    }

    private async Task DeleteItemAsync(CatalogItemDTO item)
    {
        try
        {
            StatusLabel.Text = "Deleting item...";
            
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{ApiBaseUrl}/{item.Id}");
                
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _items.Remove(item);
                    await SaveItemsLocallyAsync(_items);
                    RefreshItemsList();
                    StatusLabel.Text = $"Item '{item.Name}' deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    StatusLabel.Text = $"Error deleting item: {errorContent}";
                    await DisplayAlert("Error", $"Failed to delete item: {errorContent}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            await DisplayAlert("Error", $"Failed to delete item: {ex.Message}", "OK");
            Debug.WriteLine($"Error deleting item: {ex.Message}");
        }
    }

    private void RefreshItemsList()
    {
        ItemsCollectionView.ItemsSource = null;
        ItemsCollectionView.ItemsSource = _items;
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
                var items = JsonSerializer.Deserialize<List<CatalogItemDTO>>(json, options);
                return items;
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
            Debug.WriteLine($"Items saved locally to: {_localStoragePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving items locally: {ex.Message}");
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
            Debug.WriteLine($"Error loading items locally: {ex.Message}");
        }

        return new List<CatalogItemDTO>();
    }
}

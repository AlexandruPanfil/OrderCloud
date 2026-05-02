using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Android;

public partial class ItemDetailPage : ContentPage
{
    private CatalogItemDTO? _existingItem;
    private bool _isEditMode;

    public ItemDetailPage()
    {
        InitializeComponent();
        _isEditMode = false;
        TitleLabel.Text = "Add New Item";
    }

    public ItemDetailPage(CatalogItemDTO item)
    {
        InitializeComponent();
        _existingItem = item;
        _isEditMode = true;
        TitleLabel.Text = "Edit Item";
        LoadItemData();
    }

    private void LoadItemData()
    {
        if (_existingItem != null)
        {
            NameEntry.Text = _existingItem.Name;
            PriceEntry.Text = _existingItem.Price.ToString("F2");
            TVAEntry.Text = _existingItem.TVA;
            TenantIdEntry.Text = _existingItem.TenantId.ToString();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!ValidateInput())
        {
            return;
        }

        var item = new CatalogItemDTO
        {
            Id = _isEditMode ? _existingItem!.Id : Guid.NewGuid(),
            Name = NameEntry.Text.Trim(),
            Price = decimal.Parse(PriceEntry.Text),
            TVA = TVAEntry.Text?.Trim() ?? string.Empty,
            TenantId = Guid.Parse(TenantIdEntry.Text.Trim())
        };

        var navigationParameter = new Dictionary<string, object>
        {
            { "ReturnedItem", item },
            { "Action", _isEditMode ? "ItemUpdated" : "ItemCreated" }
        };

        // Возвращаемся на предыдущую страницу и передаем объект через параметры Shell
        await Shell.Current.GoToAsync("..", navigationParameter);
    }

    private bool ValidateInput()
    {
        ErrorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            ShowError("Name is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(PriceEntry.Text) || !decimal.TryParse(PriceEntry.Text, out decimal price))
        {
            ShowError("Please enter a valid price");
            return false;
        }

        if (price < 0)
        {
            ShowError("Price must be non-negative");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TenantIdEntry.Text) || !Guid.TryParse(TenantIdEntry.Text, out _))
        {
            ShowError("Please enter a valid Tenant ID (GUID format)");
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}


namespace OrderCloud.Android;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        ApiUrlEntry.Text = Constants.ApiBaseUrl;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var newUrl = ApiUrlEntry.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(newUrl))
        {
            await DisplayAlert("Error", "URL cannot be empty.", "OK");
            return;
        }

        Constants.ApiBaseUrl = newUrl;
        await DisplayAlert("Success", "Settings saved successfully.", "OK");
        await Navigation.PopAsync();
    }
}
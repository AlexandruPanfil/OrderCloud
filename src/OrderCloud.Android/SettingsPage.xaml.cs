namespace OrderCloud.Android;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        ApiUrlEntry.Text = Constants.ApiBaseUrl;
    }

    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
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
    }
}

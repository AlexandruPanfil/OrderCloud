namespace OrderCloud.Android;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void OnMenuClicked(object sender, EventArgs e)
	{
		if (Shell.Current is not null)
		{
			Shell.Current.FlyoutIsPresented = true;
		}
	}

	private async void OnNewSaleTapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync(AppShell.SaleRoute);
	}

	private async void OnOrdersTapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync(AppShell.OrdersRoute);
	}

	private async void OnSettingsTapped(object sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync(AppShell.SettingsRoute);
	}
}

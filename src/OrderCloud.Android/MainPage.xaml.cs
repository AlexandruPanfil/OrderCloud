namespace OrderCloud.Android;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnNewSaleTapped(object sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(new ItemsPage());
	}

	private async void OnOrdersTapped(object sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(new OrdersPage());
	}

	private async void OnSettingsTapped(object sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(new SettingsPage());
	}
}

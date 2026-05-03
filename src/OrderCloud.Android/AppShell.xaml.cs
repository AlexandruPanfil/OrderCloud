namespace OrderCloud.Android;

public partial class AppShell : Shell
{
	public const string HomeRoute = "//home";
	public const string LoginRoute = "//login";
	public const string SaleRoute = "//sale";
	public const string OrdersRoute = "//orders";
	public const string SettingsRoute = "//settings";

	public AppShell()
	{
		InitializeComponent();
		
		Routing.RegisterRoute(nameof(ItemsPage), typeof(ItemsPage));
		Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
		Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
		Routing.RegisterRoute(nameof(OrdersPage), typeof(OrdersPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}

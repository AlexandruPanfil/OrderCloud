namespace OrderCloud.Android;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		Routing.RegisterRoute(nameof(ItemsPage), typeof(ItemsPage));
		Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
	}
}

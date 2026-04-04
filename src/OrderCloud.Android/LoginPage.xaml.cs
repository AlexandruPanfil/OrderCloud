namespace OrderCloud.Android;

public partial class LoginPage : ContentPage
{
	private bool isLoggedIn = false;

	public LoginPage()
	{
		InitializeComponent();
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || 
		    string.IsNullOrWhiteSpace(PasswordEntry.Text))
		{
			await DisplayAlert("Error", "Please enter username and password", "OK");
			return;
		}

		// Add your login logic here
		isLoggedIn = true;
		LoginBtn.IsVisible = false;
		LogoutBtn.IsVisible = true;
		UsernameEntry.IsEnabled = false;
		PasswordEntry.IsEnabled = false;

		await DisplayAlert("Success", "Logged in successfully!", "OK");
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		// Add your logout logic here
		isLoggedIn = false;
		LoginBtn.IsVisible = true;
		LogoutBtn.IsVisible = false;
		UsernameEntry.IsEnabled = true;
		PasswordEntry.IsEnabled = true;
		UsernameEntry.Text = string.Empty;
		PasswordEntry.Text = string.Empty;

		await DisplayAlert("Success", "Logged out successfully!", "OK");
	}
}

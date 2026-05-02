using System.Text;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Android;

public partial class LoginPage : ContentPage
{
	private bool isLoggedIn = false;
	private string VerifyPinApiUrl => $"{Constants.ApiBaseUrl}/api/localusers/verify-pin";

	public LoginPage()
	{
		InitializeComponent();
		CheckExistingSession();
	}

	private void CheckExistingSession()
	{
		var savedUserId = Preferences.Get("LocalUserId", string.Empty);
		if (!string.IsNullOrEmpty(savedUserId))
		{
			SetIsLoggedInState(true, savedUserId);
		}
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || 
		    string.IsNullOrWhiteSpace(PasswordEntry.Text))
		{
			await DisplayAlert("Error", "Please enter Local User ID and PIN", "OK");
			return;
		}

		if (!Guid.TryParse(UsernameEntry.Text.Trim(), out var localUserId))
		{
			await DisplayAlert("Error", "Username must be a valid User ID (Guid)", "OK");
			return;
		}

		LoginBtn.IsEnabled = false;

		try
		{
			var isSuccess = await VerifyPinAsync(localUserId, PasswordEntry.Text.Trim());
			
			if (isSuccess)
			{
				Preferences.Set("LocalUserId", localUserId.ToString());
				SetIsLoggedInState(true, localUserId.ToString());
				await DisplayAlert("Success", "Logged in successfully!", "OK");
				
				// Опционально: сразу перейти на страницу Items или Orders
				// await Shell.Current.GoToAsync("//ItemsPage");
			}
			else
			{
				await DisplayAlert("Error", "Invalid User ID or PIN", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Connection failed: {ex.Message}", "OK");
		}
		finally
		{
			LoginBtn.IsEnabled = true;
		}
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		Preferences.Remove("LocalUserId");
		SetIsLoggedInState(false);
		await DisplayAlert("Success", "Logged out successfully!", "OK");
	}

	private void SetIsLoggedInState(bool loggedIn, string username = "")
	{
		isLoggedIn = loggedIn;
		LoginBtn.IsVisible = !loggedIn;
		LogoutBtn.IsVisible = loggedIn;
		UsernameEntry.IsEnabled = !loggedIn;
		PasswordEntry.IsEnabled = !loggedIn;

		if (loggedIn)
		{
			UsernameEntry.Text = username;
			PasswordEntry.Text = "••••";
		}
		else
		{
			UsernameEntry.Text = string.Empty;
			PasswordEntry.Text = string.Empty;
		}
	}

	private async Task<bool> VerifyPinAsync(Guid localUserId, string pinCode)
	{
		try
		{
			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(10);

			var request = new VerifyPinRequest
			{
				LocalUserId = localUserId,
				PinCode = pinCode
			};

			var json = JsonSerializer.Serialize(request);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(VerifyPinApiUrl, content);
			
			if (response.IsSuccessStatusCode)
			{
				var resultString = await response.Content.ReadAsStringAsync();
				return bool.TryParse(resultString, out var isValid) && isValid;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}
}

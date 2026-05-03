using System.Text;
using System.Text.Json;
using OrderCloud.Shared.Models;
using System.Net.Http.Json;

namespace OrderCloud.Android;

public partial class LoginPage : ContentPage
{
	private bool isLoggedIn = false;
	private bool isDeviceActivated = false;
	private Guid? activatedDeviceId;
	private string VerifyPinApiUrl => $"{Constants.ApiBaseUrl}/api/localusers/verify-pin";
	private string VerifyPinByDeviceApiUrl => $"{Constants.ApiBaseUrl}/api/localusers/verify-pin-by-device";
	private string DevicesApiUrl => $"{Constants.ApiBaseUrl}/api/devices";

	public LoginPage()
	{
		InitializeComponent();
		CheckDeviceActivation();
		CheckExistingSession();
	}

	private void CheckDeviceActivation()
	{
		var savedDeviceId = Constants.GetActivatedDeviceId();
		if (savedDeviceId.HasValue)
		{
			SetDeviceActivationState(true, savedDeviceId.Value);
			_ = EnsureActivatedTenantIdAsync(savedDeviceId.Value);
		}
		else
		{
			SetDeviceActivationState(false);
		}
	}

	private async Task EnsureActivatedTenantIdAsync(Guid deviceId)
	{
		if (Constants.GetActivatedTenantId().HasValue)
		{
			return;
		}

		try
		{
			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(10);
			var response = await client.GetAsync($"{DevicesApiUrl}/{deviceId}");
			if (!response.IsSuccessStatusCode)
			{
				return;
			}

			var device = await response.Content.ReadFromJsonAsync<DeviceDTO>();
			if (device != null && device.TenantId != Guid.Empty)
			{
				Constants.SetActivatedTenantId(device.TenantId);
			}
		}
		catch
		{
			// Keep app usable even if backfill fails.
		}
	}

	private void SetDeviceActivationState(bool activated, Guid? deviceId = null)
	{
		isDeviceActivated = activated;
		activatedDeviceId = activated ? deviceId : null;

		ActivationCodeEntry.IsEnabled = !activated;
		ActivateDeviceBtn.IsEnabled = !activated;
		ActivationStatusLabel.Text = activated && deviceId.HasValue
			? $"Activated device: {deviceId.Value}"
			: "Device is not activated";
		ActivationStatusLabel.TextColor = activated ? Colors.Green : Colors.Gray;

		// When the device is activated, local user id is resolved by device+pin
		UsernameEntry.IsVisible = !activated;

		if (!activated)
		{
			Constants.SetActivatedDeviceId(null);
			Constants.SetActivatedTenantId(null);
		}
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
		if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
		{
			await DisplayAlert("Error", "Please enter PIN code", "OK");
			return;
		}

		LoginBtn.IsEnabled = false;

		try
		{
			if (isDeviceActivated && activatedDeviceId.HasValue)
			{
				var verifyResponse = await VerifyPinByDeviceAsync(activatedDeviceId.Value, PasswordEntry.Text.Trim());
				if (verifyResponse?.IsValid == true && verifyResponse.LocalUserId.HasValue)
				{
					var userId = verifyResponse.LocalUserId.Value.ToString();
					Preferences.Set("LocalUserId", userId);
					SetIsLoggedInState(true, userId);
					await DisplayAlert("Success", "Logged in successfully!", "OK");
					await Shell.Current.GoToAsync(AppShell.HomeRoute);
				}
				else
				{
					await DisplayAlert("Error", "Invalid PIN for the activated device", "OK");
				}

				return;
			}

			if (string.IsNullOrWhiteSpace(UsernameEntry.Text))
			{
				await DisplayAlert("Error", "Please enter Local User ID", "OK");
				return;
			}

			if (!Guid.TryParse(UsernameEntry.Text.Trim(), out var localUserId))
			{
				await DisplayAlert("Error", "Local User ID must be a valid Guid", "OK");
				return;
			}

			var isSuccess = await VerifyPinAsync(localUserId, PasswordEntry.Text.Trim());
			
			if (isSuccess)
			{
				Preferences.Set("LocalUserId", localUserId.ToString());
				SetIsLoggedInState(true, localUserId.ToString());
				await DisplayAlert("Success", "Logged in successfully!", "OK");
				await Shell.Current.GoToAsync(AppShell.HomeRoute);
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

	private async void OnActivateDeviceClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(ActivationCodeEntry.Text))
		{
			await DisplayAlert("Error", "Please enter activation code", "OK");
			return;
		}

		if (!Guid.TryParse(ActivationCodeEntry.Text.Trim(), out var deviceId))
		{
			await DisplayAlert("Error", "Activation code must be a valid device ID (Guid)", "OK");
			return;
		}

		ActivateDeviceBtn.IsEnabled = false;

		try
		{
			var activatedDevice = await ActivateDeviceAsync(deviceId);
			if (activatedDevice == null)
			{
				await DisplayAlert("Activation Failed", "Invalid, inactive, or expired device activation code.", "OK");
				return;
			}

			Constants.SetActivatedDeviceId(activatedDevice.Id);
			Constants.SetActivatedTenantId(activatedDevice.TenantId);
			SetDeviceActivationState(true, activatedDevice.Id);
			await DisplayAlert("Success", "Device activated. You can now log in with PIN only.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Activation failed: {ex.Message}", "OK");
		}
		finally
		{
			ActivateDeviceBtn.IsEnabled = true;
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
		UsernameEntry.IsEnabled = !loggedIn && !isDeviceActivated;
		PasswordEntry.IsEnabled = !loggedIn;

		if (loggedIn)
		{
			if (!isDeviceActivated)
			{
				UsernameEntry.Text = username;
			}
			PasswordEntry.Text = "••••";
		}
		else
		{
			if (!isDeviceActivated)
			{
				UsernameEntry.Text = string.Empty;
			}
			PasswordEntry.Text = string.Empty;
		}
	}

	private async Task<DeviceDTO?> ActivateDeviceAsync(Guid deviceId)
	{
		try
		{
			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(10);

			var response = await client.GetAsync($"{DevicesApiUrl}/{deviceId}");
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			var device = await response.Content.ReadFromJsonAsync<DeviceDTO>();
			if (device == null)
			{
				return null;
			}

			var isActive = string.Equals(device.Status, "Active", StringComparison.OrdinalIgnoreCase);
			var isExpired = device.ActiveTill <= DateTime.UtcNow;
			return isActive && !isExpired ? device : null;
		}
		catch
		{
			return null;
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

	private async Task<VerifyPinByDeviceResponse?> VerifyPinByDeviceAsync(Guid deviceId, string pinCode)
	{
		try
		{
			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(10);

			var request = new VerifyPinByDeviceRequest
			{
				DeviceId = deviceId,
				PinCode = pinCode
			};

			var response = await client.PostAsJsonAsync(VerifyPinByDeviceApiUrl, request);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			return await response.Content.ReadFromJsonAsync<VerifyPinByDeviceResponse>();
		}
		catch
		{
			return null;
		}
	}
}

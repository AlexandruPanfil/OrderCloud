namespace OrderCloud.Android;

public static class Constants
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";
    private const string DefaultApiBaseUrl = "http://10.0.2.2:5126"; // Для эмулятора Android

    public static string ApiBaseUrl
    {
        get => Preferences.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Set(ApiBaseUrlKey, value);
    }
}
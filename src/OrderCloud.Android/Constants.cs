namespace OrderCloud.Android;

public static class Constants
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";
    private const string DefaultApiBaseUrl = "https://localhost:7173/"; // Для эмулятора Android
    public const string ActivatedDeviceIdKey = "ActivatedDeviceId";
    public const string ActivatedTenantIdKey = "ActivatedTenantId";

    public static string ApiBaseUrl
    {
        get => Preferences.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Set(ApiBaseUrlKey, value);
    }

    public static Guid? GetActivatedDeviceId()
    {
        var raw = Preferences.Get(ActivatedDeviceIdKey, string.Empty);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static void SetActivatedDeviceId(Guid? deviceId)
    {
        if (deviceId.HasValue)
        {
            Preferences.Set(ActivatedDeviceIdKey, deviceId.Value.ToString());
        }
        else
        {
            Preferences.Remove(ActivatedDeviceIdKey);
        }
    }

    public static Guid? GetActivatedTenantId()
    {
        var raw = Preferences.Get(ActivatedTenantIdKey, string.Empty);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static void SetActivatedTenantId(Guid? tenantId)
    {
        if (tenantId.HasValue)
        {
            Preferences.Set(ActivatedTenantIdKey, tenantId.Value.ToString());
        }
        else
        {
            Preferences.Remove(ActivatedTenantIdKey);
        }
    }
}

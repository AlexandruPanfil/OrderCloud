public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private string? _apiSecret;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public void SetCredentials(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
        _httpClient.DefaultRequestHeaders.Remove("X-API-Secret");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("X-API-Secret", apiSecret);
    }

    public HttpClient Client => _httpClient;
}
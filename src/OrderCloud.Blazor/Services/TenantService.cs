using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Blazor.Services
{
    public interface ITenantService
    {
        Task<List<TenantDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<TenantDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TenantDTO> CreateAsync(TenantDTO tenant, CancellationToken cancellationToken = default);
        Task<TenantDTO> UpdateAsync(Guid id, TenantDTO tenant, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public class TenantService : ITenantService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "/api/tenants";

        public TenantService(HttpClient http)   
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<TenantDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<TenantDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<TenantDTO>();
        }

        public async Task<TenantDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<TenantDTO>($"{BasePath}/{id}", JsonOptions, cancellationToken);
        }

        public async Task<TenantDTO> CreateAsync(TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            var resp = await _http.PostAsJsonAsync(BasePath, tenant, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<TenantDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty tenant after create.");
        }

        public async Task<TenantDTO> UpdateAsync(Guid id, TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            var resp = await _http.PutAsJsonAsync($"{BasePath}/{id}", tenant, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var updated = await resp.Content.ReadFromJsonAsync<TenantDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty tenant after update.");
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var resp = await _http.DeleteAsync($"{BasePath}/{id}", cancellationToken);
            resp.EnsureSuccessStatusCode();
        }
    }
}


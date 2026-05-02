using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Blazor.Services
{
    public interface ICatalogItemService
    {
        Task<List<CatalogItemDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CatalogItemDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CatalogItemDTO> CreateAsync(CatalogItemDTO item, CancellationToken cancellationToken = default);
        Task<CatalogItemDTO> UpdateAsync(Guid id, CatalogItemDTO item, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public class CatalogItemService : ICatalogItemService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "api/items";

        public CatalogItemService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<CatalogItemDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<CatalogItemDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<CatalogItemDTO>();
        }

        public async Task<CatalogItemDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<CatalogItemDTO>($"{BasePath}/{id}", JsonOptions, cancellationToken);
        }

        public async Task<CatalogItemDTO> CreateAsync(CatalogItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();

            item.Tenant = null;

            var response = await _http.PostAsJsonAsync(BasePath, item, cancellationToken);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<CatalogItemDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty item after create.");
        }

        public async Task<CatalogItemDTO> UpdateAsync(Guid id, CatalogItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.Tenant = null;

            var response = await _http.PutAsJsonAsync($"{BasePath}/{id}", item, cancellationToken);
            response.EnsureSuccessStatusCode();

            var updated = await response.Content.ReadFromJsonAsync<CatalogItemDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty item after update.");
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var response = await _http.DeleteAsync($"{BasePath}/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}

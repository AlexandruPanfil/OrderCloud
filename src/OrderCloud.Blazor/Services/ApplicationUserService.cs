using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Blazor.Services
{
    public interface IApplicationUserService
    {
        Task<List<ApplicationUserAssignmentDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task UpdateTenantsAsync(string userId, List<Guid> tenantIds, CancellationToken cancellationToken = default);
    }

    public class ApplicationUserService : IApplicationUserService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "/api/applicationusers";

        public ApplicationUserService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<ApplicationUserAssignmentDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<ApplicationUserAssignmentDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<ApplicationUserAssignmentDTO>();
        }

        public async Task UpdateTenantsAsync(string userId, List<Guid> tenantIds, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            tenantIds ??= new List<Guid>();

            var payload = new { TenantIds = tenantIds };
            var response = await _http.PutAsJsonAsync($"{BasePath}/{userId}/tenants", payload, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}

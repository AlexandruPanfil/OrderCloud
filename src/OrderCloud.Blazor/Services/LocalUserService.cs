using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Blazor.Services
{
    public interface ILocalUserService
    {
        Task<List<LocalUserDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<LocalUserDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<LocalUserDTO> CreateAsync(LocalUserDTO localUser, CancellationToken cancellationToken = default);
        Task<LocalUserDTO> UpdateAsync(Guid id, LocalUserDTO localUser, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public class LocalUserService : ILocalUserService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "api/localusers";

        public LocalUserService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<LocalUserDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<LocalUserDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<LocalUserDTO>();
        }

        public async Task<LocalUserDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<LocalUserDTO>($"{BasePath}/{id}", JsonOptions, cancellationToken);
        }

        public async Task<LocalUserDTO> CreateAsync(LocalUserDTO localUser, CancellationToken cancellationToken = default)
        {
            if (localUser == null)
            {
                throw new ArgumentNullException(nameof(localUser));
            }

            if (localUser.Id == Guid.Empty)
            {
                localUser.Id = Guid.NewGuid();
            }

            var response = await _http.PostAsJsonAsync(BasePath, localUser, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var created = await response.Content.ReadFromJsonAsync<LocalUserDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty local user after create.");
        }

        public async Task<LocalUserDTO> UpdateAsync(Guid id, LocalUserDTO localUser, CancellationToken cancellationToken = default)
        {
            if (localUser == null)
            {
                throw new ArgumentNullException(nameof(localUser));
            }

            var response = await _http.PutAsJsonAsync($"{BasePath}/{id}", localUser, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var updated = await response.Content.ReadFromJsonAsync<LocalUserDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty local user after update.");
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var response = await _http.DeleteAsync($"{BasePath}/{id}", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? $"Request failed with status code {(int)response.StatusCode} ({response.StatusCode})."
                : message);
        }
    }
}

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Blazor.Services
{
    public interface IDeviceService
    {
        Task<List<DeviceDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<DeviceDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DeviceDTO> CreateAsync(DeviceDTO device, CancellationToken cancellationToken = default);
        Task<DeviceDTO> UpdateAsync(Guid id, DeviceDTO device, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public class DeviceService : IDeviceService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "api/devices";

        public DeviceService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<DeviceDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<DeviceDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<DeviceDTO>();
        }

        public async Task<DeviceDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<DeviceDTO>($"{BasePath}/{id}", JsonOptions, cancellationToken);
        }

        public async Task<DeviceDTO> CreateAsync(DeviceDTO device, CancellationToken cancellationToken = default)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (device.Id == Guid.Empty) device.Id = Guid.NewGuid();

            var response = await _http.PostAsJsonAsync(BasePath, device, cancellationToken);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<DeviceDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty device after create.");
        }

        public async Task<DeviceDTO> UpdateAsync(Guid id, DeviceDTO device, CancellationToken cancellationToken = default)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            var response = await _http.PutAsJsonAsync($"{BasePath}/{id}", device, cancellationToken);
            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<DeviceDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty device after update.");
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var response = await _http.DeleteAsync($"{BasePath}/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}


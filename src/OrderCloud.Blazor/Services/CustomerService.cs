using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Blazor.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CustomerDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CustomerDTO> CreateAsync(CustomerDTO customer, CancellationToken cancellationToken = default);
        Task<CustomerDTO> UpdateAsync(Guid id, CustomerDTO customer, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "api/customers";

        public CustomerService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<CustomerDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<CustomerDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<CustomerDTO>();
        }

        public async Task<CustomerDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<CustomerDTO>($"{BasePath}/{id}", JsonOptions, cancellationToken);
        }

        public async Task<CustomerDTO> CreateAsync(CustomerDTO customer, CancellationToken cancellationToken = default)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            if (customer.Id == Guid.Empty)
            {
                customer.Id = Guid.NewGuid();
            }

            var response = await _http.PostAsJsonAsync(BasePath, customer, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var created = await response.Content.ReadFromJsonAsync<CustomerDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty customer after create.");
        }

        public async Task<CustomerDTO> UpdateAsync(Guid id, CustomerDTO customer, CancellationToken cancellationToken = default)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            var response = await _http.PutAsJsonAsync($"{BasePath}/{id}", customer, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var updated = await response.Content.ReadFromJsonAsync<CustomerDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty customer after update.");
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
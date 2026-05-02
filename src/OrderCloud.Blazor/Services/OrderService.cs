using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OrderCloud.Shared.Models;

namespace OrderCloud.Blazor.Services
{
    public interface IOrderService
    {
        Task<List<OrderDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<OrderDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OrderDTO> CreateAsync(OrderDTO order, CancellationToken cancellationToken = default);
        Task<OrderDTO> UpdateAsync(Guid id, OrderDTO order, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        // Item-related helpers (optional endpoints: api/orders/{orderId}/items)
        Task<ItemDTO> AddItemAsync(Guid orderId, ItemDTO item, CancellationToken cancellationToken = default);
        Task<ItemDTO> UpdateItemAsync(Guid orderId, Guid itemId, ItemDTO item, CancellationToken cancellationToken = default);
        Task DeleteItemAsync(Guid orderId, Guid itemId, CancellationToken cancellationToken = default);
    }

    public class OrderService : IOrderService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string BasePath = "api/orders";

        public OrderService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<List<OrderDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _http.GetFromJsonAsync<List<OrderDTO>>(BasePath, JsonOptions, cancellationToken);
            return result ?? new List<OrderDTO>();
        }

        public async Task<OrderDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync<OrderDTO>($"{BasePath}/{id}", JsonOptions, cancellationToken);
        }

        public async Task<OrderDTO> CreateAsync(OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.Id == Guid.Empty) order.Id = Guid.NewGuid();

            var resp = await _http.PostAsJsonAsync(BasePath, order, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<OrderDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty order after create.");
        }

        public async Task<OrderDTO> UpdateAsync(Guid id, OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            var resp = await _http.PutAsJsonAsync($"{BasePath}/{id}", order, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var updated = await resp.Content.ReadFromJsonAsync<OrderDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty order after update.");
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var resp = await _http.DeleteAsync($"{BasePath}/{id}", cancellationToken);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<ItemDTO> AddItemAsync(Guid orderId, ItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();

            var resp = await _http.PostAsJsonAsync($"{BasePath}/{orderId}/items", item, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<ItemDTO>(JsonOptions, cancellationToken);
            return created ?? throw new InvalidOperationException("Server returned empty item after create.");
        }

        public async Task<ItemDTO> UpdateItemAsync(Guid orderId, Guid itemId, ItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var resp = await _http.PutAsJsonAsync($"{BasePath}/{orderId}/items/{itemId}", item, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var updated = await resp.Content.ReadFromJsonAsync<ItemDTO>(JsonOptions, cancellationToken);
            return updated ?? throw new InvalidOperationException("Server returned empty item after update.");
        }

        public async Task DeleteItemAsync(Guid orderId, Guid itemId, CancellationToken cancellationToken = default)
        {
            var resp = await _http.DeleteAsync($"{BasePath}/{orderId}/items/{itemId}", cancellationToken);
            resp.EnsureSuccessStatusCode();
        }
    }
}


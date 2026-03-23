using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using OrderCloud.Blazor.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        // Простой потокобезопасный in-memory store — замените на репозиторий/DbContext при необходимости.
        private static readonly ConcurrentDictionary<Guid, OrderDTO> Store = new();

        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ILogger<OrdersController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<OrderDTO>> GetAll(CancellationToken cancellationToken = default)
        {
            return Ok(Store.Values);
        }

        [HttpGet("{id:guid}")]
        public ActionResult<OrderDTO> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            if (Store.TryGetValue(id, out var order))
                return Ok(order);

            return NotFound();
        }

        [HttpPost]
        public ActionResult<OrderDTO> Create([FromBody] OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null) return BadRequest();

            if (order.Id == Guid.Empty) order.Id = Guid.NewGuid();
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = order.CreatedAt;
            order.Items ??= new List<ItemDTO>();
            RecalculateOrderTotal(order);

            if (!Store.TryAdd(order.Id, order))
            {
                _logger.LogWarning("Order with id {Id} already exists.", order.Id);
                return Conflict($"Order with id {order.Id} already exists.");
            }

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPut("{id:guid}")]
        public ActionResult<OrderDTO> Update(Guid id, [FromBody] OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null) return BadRequest();
            if (id == Guid.Empty) return BadRequest();
            if (!Store.ContainsKey(id)) return NotFound();

            order.Id = id;
            order.UpdatedAt = DateTime.UtcNow;
            order.Items ??= new List<ItemDTO>();
            RecalculateOrderTotal(order);
            Store[id] = order;
            return Ok(order);
        }

        [HttpDelete("{id:guid}")]
        public ActionResult Delete(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty) return BadRequest();

            if (!Store.TryRemove(id, out _)) return NotFound();

            return NoContent();
        }

        // Items endpoints

        [HttpPost("{orderId:guid}/items")]
        public ActionResult<ItemDTO> AddItem(Guid orderId, [FromBody] ItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null) return BadRequest();
            if (!Store.TryGetValue(orderId, out var order)) return NotFound();

            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.OrderId = orderId;
            item.Total = item.Price * item.Quantity;
            item.Order = null; // avoid circular references in response

            order.Items ??= new List<ItemDTO>();
            order.Items.Add(item);
            order.UpdatedAt = DateTime.UtcNow;
            RecalculateOrderTotal(order);
            Store[orderId] = order;

            return CreatedAtAction(nameof(GetById), new { id = orderId }, item);
        }

        [HttpPut("{orderId:guid}/items/{itemId:guid}")]
        public ActionResult<ItemDTO> UpdateItem(Guid orderId, Guid itemId, [FromBody] ItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null) return BadRequest();
            if (!Store.TryGetValue(orderId, out var order)) return NotFound();

            order.Items ??= new List<ItemDTO>();
            var existing = order.Items.FirstOrDefault(i => i.Id == itemId);
            if (existing == null) return NotFound();

            // Update fields
            existing.Name = item.Name;
            existing.Price = item.Price;
            existing.Quantity = item.Quantity;
            existing.Total = existing.Price * existing.Quantity;
            existing.TVA = item.TVA;
            order.UpdatedAt = DateTime.UtcNow;
            RecalculateOrderTotal(order);
            Store[orderId] = order;

            return Ok(existing);
        }

        [HttpDelete("{orderId:guid}/items/{itemId:guid}")]
        public ActionResult DeleteItem(Guid orderId, Guid itemId, CancellationToken cancellationToken = default)
        {
            if (!Store.TryGetValue(orderId, out var order)) return NotFound();

            order.Items ??= new List<ItemDTO>();
            var removed = order.Items.RemoveAll(i => i.Id == itemId) > 0;
            if (!removed) return NotFound();

            order.UpdatedAt = DateTime.UtcNow;
            RecalculateOrderTotal(order);
            Store[orderId] = order;
            return NoContent();
        }

        // Вспомогательный метод пересчёта суммы заказа.
        private static void RecalculateOrderTotal(OrderDTO order)
        {
            order.Total = order.Items?.Sum(i => i.Total) ?? 0m;
        }
    }
}

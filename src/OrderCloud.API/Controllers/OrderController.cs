using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Blazor.Data;
using OrderCloud.Blazor.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext db, ILogger<OrdersController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var orders = await _db.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDTO>> Create([FromBody] OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null) return BadRequest();
            if (order.Id == Guid.Empty) order.Id = Guid.NewGuid();
            if (!await TryValidateReferencesAsync(order, cancellationToken))
            {
                return ValidationProblem(ModelState);
            }

            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = order.CreatedAt;
            order.Items ??= new List<ItemDTO>();

            // Отсоединяем связанные сущности
            order.Tenant = null;
            order.Customer = null;
            order.LocalUser = null;

            foreach (var item in order.Items)
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                item.OrderId = order.Id;
                item.Total = item.Price * item.Quantity;
                item.Order = null;
            }

            order.Total = order.Items.Sum(i => i.Total);

            // Если связанных объектов не существует в БД, EF выдаст FK constraint ошибку.
            // Поэтому перед сохранением заказа убедимся, что мы их НЕ трекаем.
            _db.Orders.Add(order);
            
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving order");
                return Problem(detail: ex.InnerException?.Message ?? ex.Message, statusCode: 500);
            }

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<OrderDTO>> Update(Guid id, [FromBody] OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null || id == Guid.Empty) return BadRequest();
            if (!await TryValidateReferencesAsync(order, cancellationToken))
            {
                return ValidationProblem(ModelState);
            }

            var existing = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (existing == null) return NotFound();

            // Map allowed fields
            existing.Status = order.Status;
            existing.TenantId = order.TenantId;
            existing.LocalUserId = order.LocalUserId;
            existing.CustomerId = order.CustomerId;
            existing.UpdatedAt = DateTime.UtcNow;

            // Replace items (simple approach). Ensure FK and totals are correct and prevent EF re-inserting nav props.
            existing.Items = order.Items ?? new List<ItemDTO>();
            foreach (var item in existing.Items)
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                item.OrderId = existing.Id;
                item.Total = item.Price * item.Quantity;
                item.Order = null!;
            }

            // Do not attach navigation objects for related existing entities
            existing.Tenant = null!;
            existing.Customer = null!;
            existing.LocalUser = null;

            existing.Total = existing.Items.Sum(i => i.Total);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating order");
                return Problem(detail: ex.InnerException?.Message ?? ex.Message, statusCode: 500);
            }

            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Orders.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return NotFound();

            _db.Orders.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        // Items endpoints delegate to EF as needed (omitted for brevity — implement similarly to in-memory version but using _db and SaveChanges)

        private async Task<bool> TryValidateReferencesAsync(OrderDTO order, CancellationToken cancellationToken)
        {
            if (order.TenantId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(order.TenantId), "Tenant is required.");
            }
            else if (!await _db.Tenants.AnyAsync(t => t.Id == order.TenantId, cancellationToken))
            {
                ModelState.AddModelError(nameof(order.TenantId), "Selected tenant was not found.");
            }

            if (order.CustomerId == Guid.Empty)
            {
                order.CustomerId = null;
            }

            if (order.LocalUserId == Guid.Empty)
            {
                order.LocalUserId = null;
            }

            if (order.CustomerId.HasValue &&
                !await _db.Customers.AnyAsync(c => c.Id == order.CustomerId.Value, cancellationToken))
            {
                ModelState.AddModelError(nameof(order.CustomerId), "Selected customer was not found.");
            }

            if (order.LocalUserId.HasValue &&
                !await _db.LocalUsers.AnyAsync(u => u.Id == order.LocalUserId.Value, cancellationToken))
            {
                ModelState.AddModelError(nameof(order.LocalUserId), "Selected local user was not found.");
            }

            return ModelState.IsValid;
        }
    }
}

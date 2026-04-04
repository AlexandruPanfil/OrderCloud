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

            foreach (var order in orders)
            {
                if (order.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        item.Order = null;
                    }
                }
            }

            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.Customer)
                .Include(o => o.Tenant)
                .Include(o => o.LocalUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order == null) return NotFound();

            // Для избежания циклов сериализации отключаем обратные ссылки у дочерних объектов перед отправкой ответа.
            if (order.Items != null)
            {
                foreach (var item in order.Items)
                {
                    item.Order = null;
                }
            }

            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDTO>> Create([FromBody] OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null) return BadRequest();
            if (order.Id == Guid.Empty) order.Id = Guid.NewGuid();

            // Validate that we have proper Tenant and LocalUser
            if (order.Tenant == null || order.Tenant.Id == Guid.Empty)
            {
                return BadRequest("Tenant is required.");
            }

            if (order.LocalUser == null || order.LocalUser.Id == Guid.Empty)
            {
                return BadRequest("Local User is required.");
            }

            // Set keys
            order.TenantId = order.Tenant.Id;
            order.LocalUserId = order.LocalUser.Id;

            // Handle Customer creation if provided
            if (order.Customer != null && !string.IsNullOrWhiteSpace(order.Customer.Name))
            {
                if (order.Customer.Id == Guid.Empty)
                {
                    order.Customer.Id = Guid.NewGuid();
                }

                // Attach customer to EF context if it's new
                var existingCustomer = await _db.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == order.Customer.Id, cancellationToken);
                
                if (existingCustomer == null)
                {
                    _db.Customers.Add(order.Customer);
                }

                order.CustomerId = order.Customer.Id;
            }
            else
            {
                // FALLBACK ONLY: If DB has NOT NULL on CustomerId, we must provide one. 
                // We create a dummy "Walk-in Customer"
                var defaultCustomer = await _db.Customers.FirstOrDefaultAsync(c => c.Name == "Walk-in", cancellationToken);
                if (defaultCustomer == null)
                {
                    defaultCustomer = new CustomerDTO { Id = Guid.NewGuid(), Name = "Walk-in" };
                    _db.Customers.Add(defaultCustomer);
                }

                order.Customer = null;
                order.CustomerId = defaultCustomer.Id;
            }

            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = order.CreatedAt;
            order.Items ??= new List<ItemDTO>();

            // Detach objects that should not be re-created
            order.Tenant = null!;
            order.LocalUser = null;
            order.Customer = null;

            foreach (var item in order.Items)
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                item.OrderId = order.Id;
                item.Total = item.Price * item.Quantity;
                item.Order = null;
            }

            order.Total = order.Items.Sum(i => i.Total);

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

            // Перед возвратом результата важно занулить ссылки на заказ внутри items,
            // если savechanges модифицировал объекты и восстановил связи.
            foreach (var item in order.Items)
            {
                item.Order = null;
            }

            // Fetch the fully constructed order without navigation properties holding cycles
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, new OrderDTO
            {
                Id = order.Id,
                Status = order.Status,
                Total = order.Total,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                TenantId = order.TenantId,
                LocalUserId = order.LocalUserId,
                CustomerId = order.CustomerId,
                Items = order.Items
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<OrderDTO>> Update(Guid id, [FromBody] OrderDTO order, CancellationToken cancellationToken = default)
        {
            if (order == null || id == Guid.Empty) return BadRequest();
            if (!await PrepareOrderForSaveAsync(order, cancellationToken))
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

            foreach (var item in existing.Items)
            {
                item.Order = null;
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

        private async Task<bool> PrepareOrderForSaveAsync(OrderDTO order, CancellationToken cancellationToken)
        {
            var tenant = await ResolveTenantAsync(order, cancellationToken);
            var localUser = tenant == null
                ? null
                : await ResolveLocalUserAsync(order, tenant.Id, cancellationToken);

            if (tenant == null || localUser == null)
            {
                return false;
            }

            order.TenantId = tenant.Id;
            order.LocalUserId = localUser.Id;

            await ResolveCustomerAsync(order, cancellationToken);

            order.Tenant = null;
            order.LocalUser = null;
            order.Customer = null;

            return ModelState.IsValid;
        }

        private async Task<TenantDTO?> ResolveTenantAsync(OrderDTO order, CancellationToken cancellationToken)
        {
            var apiKey = order.Tenant?.ApiKey;
            var apiSecret = order.Tenant?.ApiSecret;

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                ModelState.AddModelError(nameof(order.Tenant), "Tenant api key and secret key are required.");
                return null;
            }

            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.ApiKey == apiKey && t.ApiSecret == apiSecret,
                    cancellationToken);

            if (tenant == null)
            {
                ModelState.AddModelError(nameof(order.Tenant), "Tenant with the provided api key and secret key was not found.");
            }

            return tenant;
        }

        private async Task<LocalUserDTO?> ResolveLocalUserAsync(OrderDTO order, Guid tenantId, CancellationToken cancellationToken)
        {
            LocalUserDTO? localUser = null;
            var localUserId = order.LocalUserId;

            if (localUserId.HasValue && localUserId.Value == Guid.Empty)
            {
                order.LocalUserId = null;
                localUserId = null;
            }

            if (localUserId.HasValue)
            {
                localUser = await _db.LocalUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.Id == localUserId.Value && u.TenantId == tenantId,
                        cancellationToken);
            }

            if (localUser == null &&
                order.LocalUser?.Id != Guid.Empty &&
                order.LocalUser?.Id != null)
            {
                localUser = await _db.LocalUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.Id == order.LocalUser!.Id && u.TenantId == tenantId,
                        cancellationToken);
            }

            if (localUser == null &&
                order.LocalUser != null &&
                !string.IsNullOrWhiteSpace(order.LocalUser.Name) &&
                !string.IsNullOrWhiteSpace(order.LocalUser.PinCode))
            {
                localUser = await _db.LocalUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.TenantId == tenantId &&
                             u.Name == order.LocalUser.Name &&
                             u.PinCode == order.LocalUser.PinCode,
                        cancellationToken);
            }

            if (localUser == null)
            {
                ModelState.AddModelError(nameof(order.LocalUser), "A valid local user is required for the selected tenant.");
            }

            return localUser;
        }

        private async Task ResolveCustomerAsync(OrderDTO order, CancellationToken cancellationToken)
        {
            if (order.CustomerId.HasValue && order.CustomerId.Value == Guid.Empty)
            {
                order.CustomerId = null;
            }

            if (order.CustomerId.HasValue)
            {
                var existingById = await _db.Customers
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == order.CustomerId.Value, cancellationToken);

                if (existingById)
                {
                    return;
                }
            }

            if (order.Customer != null && order.Customer.Id != Guid.Empty)
            {
                var existingFromPayload = await _db.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == order.Customer.Id, cancellationToken);

                if (existingFromPayload != null)
                {
                    order.CustomerId = existingFromPayload.Id;
                    return;
                }
            }

            if (order.Customer == null || string.IsNullOrWhiteSpace(order.Customer.Name))
            {
                order.Customer = null;
                order.CustomerId = null;
                return;
            }

            var customer = new CustomerDTO
            {
                Id = order.Customer.Id == Guid.Empty ? Guid.NewGuid() : order.Customer.Id,
                Name = string.IsNullOrWhiteSpace(order.Customer.Name) ? "Unknown customer" : order.Customer.Name,
                IDNO = order.Customer.IDNO
            };

            _db.Customers.Add(customer);
            order.CustomerId = customer.Id;
        }
    }
}

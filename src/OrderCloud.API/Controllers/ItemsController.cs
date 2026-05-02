using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;
using OrderCloud.Shared.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(ApplicationDbContext db, ILogger<ItemsController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatalogItemDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var items = await _db.CatalogItems
                .Include(i => i.Tenant)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CatalogItemDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var item = await _db.CatalogItems
                .Include(i => i.Tenant)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CatalogItemDTO>> Create([FromBody] CatalogItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null)
            {
                return BadRequest();
            }

            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest("Name is required.");
            }

            if (item.Price < 0)
            {
                return BadRequest("Price must be non-negative.");
            }

            if (item.TenantId == Guid.Empty)
            {
                return BadRequest("Tenant is required.");
            }

            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == item.TenantId, cancellationToken);
            if (!tenantExists)
            {
                return BadRequest("Selected tenant was not found.");
            }

            item.Tenant = null;

            _db.CatalogItems.Add(item);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving item in Create");
                return StatusCode(500, "Error saving item");
            }

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CatalogItemDTO>> Update(Guid id, [FromBody] CatalogItemDTO item, CancellationToken cancellationToken = default)
        {
            if (item == null || id == Guid.Empty)
            {
                return BadRequest();
            }

            var existing = await _db.CatalogItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
            if (existing == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest("Name is required.");
            }

            if (item.Price < 0)
            {
                return BadRequest("Price must be non-negative.");
            }

            if (item.TenantId == Guid.Empty)
            {
                return BadRequest("Tenant is required.");
            }

            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == item.TenantId, cancellationToken);
            if (!tenantExists)
            {
                return BadRequest("Selected tenant was not found.");
            }

            existing.Name = item.Name;
            existing.Price = item.Price;
            existing.TVA = item.TVA;
            existing.TenantId = item.TenantId;
            existing.Tenant = null;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving item in Update");
                return StatusCode(500, "Error updating item");
            }

            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.CatalogItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
            if (existing == null)
            {
                return NotFound();
            }

            _db.CatalogItems.Remove(existing);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item");
                return StatusCode(500, "Error deleting item");
            }

            return NoContent();
        }
    }
}

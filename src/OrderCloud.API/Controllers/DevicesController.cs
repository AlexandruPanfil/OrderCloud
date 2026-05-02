using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;
using OrderCloud.Shared.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(ApplicationDbContext db, ILogger<DevicesController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<ActionResult<DeviceDTO>> Create([FromBody] DeviceDTO device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                return BadRequest();
            }

            if (device.Id == Guid.Empty)
            {
                device.Id = Guid.NewGuid();
            }

            if (device.TenantId == Guid.Empty)
            {
                return BadRequest("Tenant is required.");
            }

            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == device.TenantId, cancellationToken);
            if (!tenantExists)
            {
                return BadRequest("Selected tenant was not found.");
            }

            device.Tenant = null;

            _db.Devices.Add(device);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Device in Create");
                return StatusCode(500, "Error saving device");
            }

            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeviceDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var devices = await _db.Devices
                .Include(d => d.Tenant)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Ok(devices);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DeviceDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var device = await _db.Devices
                .Include(d => d.Tenant)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (device == null)
            {
                return NotFound();
            }

            return Ok(device);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DeviceDTO>> Update(Guid id, [FromBody] DeviceDTO device, CancellationToken cancellationToken = default)
        {
            if (device == null || id == Guid.Empty)
            {
                return BadRequest();
            }

            var existing = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (existing == null)
            {
                return NotFound();
            }

            if (device.TenantId == Guid.Empty)
            {
                return BadRequest("Tenant is required.");
            }

            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == device.TenantId, cancellationToken);
            if (!tenantExists)
            {
                return BadRequest("Selected tenant was not found.");
            }

            existing.Name = device.Name;
            existing.Status = device.Status;
            existing.ActiveTill = device.ActiveTill;
            existing.TenantId = device.TenantId;
            existing.Tenant = null!;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Device in Update");
                return StatusCode(500, "Error updating device");
            }

            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (existing == null)
            {
                return NotFound();
            }

            _db.Devices.Remove(existing);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Device");
                return StatusCode(500, "Error deleting device");
            }

            return NoContent();
        }
    }
}

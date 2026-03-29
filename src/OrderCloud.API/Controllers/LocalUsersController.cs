using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Blazor.Data;
using OrderCloud.Blazor.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<LocalUsersController> _logger;

        public LocalUsersController(ApplicationDbContext db, ILogger<LocalUsersController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocalUserDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var localUsers = await _db.LocalUsers
                .Include(u => u.Device)
                .Include(u => u.Tenant)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Ok(localUsers);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<LocalUserDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var localUser = await _db.LocalUsers
                .Include(u => u.Device)
                .Include(u => u.Tenant)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (localUser == null)
            {
                return NotFound();
            }

            return Ok(localUser);
        }

        [HttpPost]
        public async Task<ActionResult<LocalUserDTO>> Create([FromBody] LocalUserDTO localUser, CancellationToken cancellationToken = default)
        {
            if (!await ValidateAsync(localUser, cancellationToken))
            {
                return BadRequest(GetModelStateErrors());
            }

            if (localUser.Id == Guid.Empty)
            {
                localUser.Id = Guid.NewGuid();
            }

            localUser.Device = null;
            localUser.Tenant = null;

            _db.LocalUsers.Add(localUser);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving local user");
                return Problem(detail: ex.InnerException?.Message ?? ex.Message, statusCode: 500);
            }

            return CreatedAtAction(nameof(GetById), new { id = localUser.Id }, localUser);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<LocalUserDTO>> Update(Guid id, [FromBody] LocalUserDTO localUser, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty || localUser == null)
            {
                return BadRequest();
            }

            if (!await ValidateAsync(localUser, cancellationToken))
            {
                return BadRequest(GetModelStateErrors());
            }

            var existing = await _db.LocalUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = localUser.Name;
            existing.PinCode = localUser.PinCode;
            existing.TenantId = localUser.TenantId;
            existing.DeviceId = localUser.DeviceId;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating local user");
                return Problem(detail: ex.InnerException?.Message ?? ex.Message, statusCode: 500);
            }

            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.LocalUsers.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
            {
                return NotFound();
            }

            var hasOrders = await _db.Orders.AnyAsync(o => o.LocalUserId == id, cancellationToken);
            if (hasOrders)
            {
                return Conflict("Cannot delete this local user because one or more orders reference it.");
            }

            _db.LocalUsers.Remove(existing);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting local user");
                return Problem(detail: ex.InnerException?.Message ?? ex.Message, statusCode: 500);
            }

            return NoContent();
        }

        private async Task<bool> ValidateAsync(LocalUserDTO localUser, CancellationToken cancellationToken)
        {
            if (localUser == null)
            {
                ModelState.AddModelError(string.Empty, "Local user payload is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(localUser.Name))
            {
                ModelState.AddModelError(nameof(localUser.Name), "Name is required.");
            }

            if (string.IsNullOrWhiteSpace(localUser.PinCode))
            {
                ModelState.AddModelError(nameof(localUser.PinCode), "PIN code is required.");
            }

            if (localUser.TenantId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(localUser.TenantId), "Tenant is required.");
            }
            else
            {
                var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == localUser.TenantId, cancellationToken);
                if (!tenantExists)
                {
                    ModelState.AddModelError(nameof(localUser.TenantId), "Selected tenant was not found.");
                }
            }

            if (localUser.DeviceId.HasValue)
            {
                if (localUser.DeviceId.Value == Guid.Empty)
                {
                    localUser.DeviceId = null;
                }
                else
                {
                    var device = await _db.Devices.AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == localUser.DeviceId.Value, cancellationToken);

                    if (device == null)
                    {
                        ModelState.AddModelError(nameof(localUser.DeviceId), "Selected device was not found.");
                    }
                    else if (localUser.TenantId != Guid.Empty && device.TenantId != localUser.TenantId)
                    {
                        ModelState.AddModelError(nameof(localUser.DeviceId), "Selected device must belong to the same tenant.");
                    }
                }
            }

            return ModelState.IsValid;
        }

        private string GetModelStateErrors()
        {
            return string.Join(" ",
                ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
        }
    }
}
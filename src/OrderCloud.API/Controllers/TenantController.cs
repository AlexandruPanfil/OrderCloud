using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;
using OrderCloud.Shared.Models;

namespace OrderCloud.API.Controllers
{
    public class TenantResponseDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string? ApiSecret { get; set; }
        public string? ApplicationUserId { get; set; }
        public List<string> ApplicationUserIds { get; set; } = new();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TenantsController> _logger;

        public TenantsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<TenantsController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantResponseDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var list = await _db.Tenants
                .AsNoTracking()
                .Include(t => t.ApplicationUsers)
                .ToListAsync(cancellationToken);

            var responseList = list.Select(t => new TenantResponseDTO
            {
                Id = t.Id,
                Name = t.Name,
                ApiKey = t.ApiKey,
                ApiSecret = t.ApiSecret,
                ApplicationUserId = t.ApplicationUsers.Select(user => user.Id).FirstOrDefault() ?? t.ApplicationUserId,
                ApplicationUserIds = t.ApplicationUsers.Select(user => user.Id).ToList()
            });
            return Ok(responseList);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TenantResponseDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var tenant = await _db.Tenants
                .Include(t => t.ApplicationUsers)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (tenant == null) return NotFound();

            var response = new TenantResponseDTO
            {
                Id = tenant.Id,
                Name = tenant.Name,
                ApiKey = tenant.ApiKey,
                ApiSecret = tenant.ApiSecret,
                ApplicationUserId = tenant.ApplicationUsers.Select(user => user.Id).FirstOrDefault() ?? tenant.ApplicationUserId,
                ApplicationUserIds = tenant.ApplicationUsers.Select(user => user.Id).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<TenantResponseDTO>> Create([FromBody] TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null) return BadRequest();

            var requestedUserIds = tenant.ApplicationUserIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedUserIds.Count == 0 && !string.IsNullOrWhiteSpace(tenant.ApplicationUserId))
            {
                requestedUserIds.Add(tenant.ApplicationUserId);
            }

            var assignedUsers = requestedUserIds.Count == 0
                ? new List<ApplicationUser>()
                : await _userManager.Users
                    .Where(user => requestedUserIds.Contains(user.Id))
                    .ToListAsync(cancellationToken);

            var missingUsers = requestedUserIds
                .Except(assignedUsers.Select(user => user.Id), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingUsers.Count != 0)
            {
                return NotFound($"User(s) not found: {string.Join(", ", missingUsers)}");
            }

            var tenantToCreate = new TenantDTO
            {
                Id = tenant.Id == Guid.Empty ? Guid.NewGuid() : tenant.Id,
                Name = tenant.Name,
                ApiKey = string.IsNullOrWhiteSpace(tenant.ApiKey)
                ? GenerateBase64Url(24)
                : tenant.ApiKey,
                ApiSecret = string.IsNullOrWhiteSpace(tenant.ApiSecret)
                ? GenerateBase64Url(48)
                : tenant.ApiSecret,
                ApplicationUserId = assignedUsers.Select(user => user.Id).FirstOrDefault() ?? tenant.ApplicationUserId
            };

            foreach (var assignedUser in assignedUsers)
            {
                tenantToCreate.ApplicationUsers.Add(assignedUser);
            }

            _db.Tenants.Add(tenantToCreate);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Tenant in Create");
                return StatusCode(500, "Error saving tenant");
            }

            // Возвращаем TenantResponseDTO, добавляя ApiSecret только один раз для того, чтобы пользователь мог его сохранить
            var response = new TenantResponseDTO
            {
                Id = tenantToCreate.Id,
                Name = tenantToCreate.Name,
                ApiKey = tenantToCreate.ApiKey,
                ApiSecret = tenantToCreate.ApiSecret, // Исключение: возвращаем один раз при создании
                ApplicationUserId = tenantToCreate.ApplicationUserId,
                ApplicationUserIds = assignedUsers.Select(user => user.Id).ToList()
            };

            return CreatedAtAction(nameof(GetById), new { id = tenantToCreate.Id }, response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TenantResponseDTO>> Update(Guid id, [FromBody] TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null || id == Guid.Empty) return BadRequest();

            var existing = await _db.Tenants.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return NotFound();

            existing.Name = tenant.Name;
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Tenant in Update");
                return StatusCode(500, "Error updating tenant");
            }

            var response = new TenantResponseDTO
            {
                Id = existing.Id,
                Name = existing.Name,
                ApiKey = existing.ApiKey,
                ApiSecret = null, // Не возвращаем секрет при обновлении
                ApplicationUserId = existing.ApplicationUserId,
                ApplicationUserIds = await _db.Tenants
                    .Where(t => t.Id == existing.Id)
                    .SelectMany(t => t.ApplicationUsers.Select(user => user.Id))
                    .ToListAsync(cancellationToken)
            };

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Tenants.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return NotFound();

            _db.Tenants.Remove(existing);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Tenant");
                return StatusCode(500, "Error deleting tenant");
            }

            return NoContent();
        }

        private static string GenerateBase64Url(int bytes)
        {
            var buffer = new byte[bytes];
            RandomNumberGenerator.Fill(buffer);
            var base64 = Convert.ToBase64String(buffer);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}


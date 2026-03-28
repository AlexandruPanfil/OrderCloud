using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Blazor.Data;
using OrderCloud.Blazor.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TenantsController> _logger;

        public TenantsController(ApplicationDbContext db, ILogger<TenantsController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var list = await _db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TenantDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var tenant = await _db.Tenants.FindAsync(new object[] { id }, cancellationToken);
            if (tenant == null) return NotFound();
            return Ok(tenant);
        }

        [HttpPost]
        public async Task<ActionResult<TenantDTO>> Create([FromBody] TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null) return BadRequest();

            if (tenant.Id == Guid.Empty) tenant.Id = Guid.NewGuid();

            tenant.ApiKey = string.IsNullOrWhiteSpace(tenant.ApiKey)
                ? GenerateBase64Url(24)
                : tenant.ApiKey;
            tenant.ApiSecret = string.IsNullOrWhiteSpace(tenant.ApiSecret)
                ? GenerateBase64Url(48)
                : tenant.ApiSecret;

            _db.Tenants.Add(tenant);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Tenant in Create");
                return StatusCode(500, "Error saving tenant");
            }

            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TenantDTO>> Update(Guid id, [FromBody] TenantDTO tenant, CancellationToken cancellationToken = default)
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

            return Ok(existing);
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

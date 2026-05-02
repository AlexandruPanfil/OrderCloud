using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;
using OrderCloud.Shared.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationUsersController> _logger;

        public ApplicationUsersController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<ApplicationUsersController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<List<ApplicationUserAssignmentDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var tenants = await _db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
            var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync(cancellationToken);

            var response = users.Select(user => new ApplicationUserAssignmentDTO
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                TenantIds = tenants.Where(t => t.ApplicationUserId == user.Id).Select(t => t.Id).ToList()
            }).ToList();

            return Ok(response);
        }

        [HttpPut("{userId}/tenants")]
        public async Task<ActionResult> UpdateTenants(string userId, [FromBody] TenantAssignmentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("User identifier is required.");
            if (request == null) return BadRequest("TenantIds payload is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound($"User '{userId}' not found.");

            var tenantIds = request.TenantIds?.Distinct().ToList() ?? new List<Guid>();
            List<TenantDTO> tenantsToAssign = new();

            if (tenantIds.Count > 0)
            {
                tenantsToAssign = await _db.Tenants
                    .Where(t => tenantIds.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var missing = tenantIds.Except(tenantsToAssign.Select(t => t.Id)).ToList();
                if (missing.Any())
                {
                    return NotFound($"Tenant(s) not found: {string.Join(", ", missing)}");
                }
            }

            var currentlyAssigned = await _db.Tenants
                .Where(t => t.ApplicationUserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var tenant in currentlyAssigned.Where(t => !tenantIds.Contains(t.Id)))
            {
                tenant.ApplicationUserId = null;
            }

            foreach (var tenant in tenantsToAssign)
            {
                tenant.ApplicationUserId = userId;
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to update tenant assignments for {UserId}", userId);
                return StatusCode(500, "Unable to update tenant assignments.");
            }

            return NoContent();
        }

        public class TenantAssignmentRequest
        {
            public List<Guid> TenantIds { get; set; } = new();
        }
    }
}


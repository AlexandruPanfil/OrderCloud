using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Blazor.Data;
using OrderCloud.Blazor.Models;

namespace OrderCloud.API.Controllers
{
    public class DashboardDTO
    {
        public int OrdersToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public int ActiveDevices { get; set; }
        public List<OrderDTO> RecentOrders { get; set; } = new();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext db, ILogger<DashboardController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDTO>> GetStatistics([FromQuery] Guid? tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest("Tenant ID is required.");
            }

            try
            {
                var today = DateTime.UtcNow.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

                // Orders today
                var ordersTodayCount = await _db.Orders
                    .Where(o => o.TenantId == tenantId && o.CreatedAt >= today)
                    .CountAsync(cancellationToken);

                // Revenue this week
                var revenueThisWeek = await _db.Orders
                    .Where(o => o.TenantId == tenantId && o.CreatedAt >= startOfWeek)
                    .SumAsync(o => o.Total, cancellationToken);

                // Active devices
                var activeDevicesCount = await _db.Devices
                    .Where(d => d.TenantId == tenantId && d.Status == "Active")
                    .CountAsync(cancellationToken);

                // Recent 5 orders
                var recentOrders = await _db.Orders
                    .Include(o => o.Customer)
                    .Where(o => o.TenantId == tenantId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new OrderDTO
                    {
                        Id = o.Id,
                        Total = o.Total,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        Customer = o.Customer != null ? new CustomerDTO { Name = o.Customer.Name } : null
                    })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var dashboard = new DashboardDTO
                {
                    OrdersToday = ordersTodayCount,
                    RevenueThisWeek = revenueThisWeek,
                    ActiveDevices = activeDevicesCount,
                    RecentOrders = recentOrders
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard statistics for tenant {TenantId}", tenantId);
                return StatusCode(500, "Error retrieving statistics");
            }
        }
    }
}
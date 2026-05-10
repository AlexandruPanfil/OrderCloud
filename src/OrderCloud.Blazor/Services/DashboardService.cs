using OrderCloud.Shared.Models;

namespace OrderCloud.Blazor.Services
{
    public class DashboardDTO
    {
        public int OrdersToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public int ActiveDevices { get; set; }
        public List<OrderDTO> RecentOrders { get; set; } = new();
    }

    public interface IDashboardService
    {
        Task<DashboardDTO?> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }

    public class DashboardService : IDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(HttpClient httpClient, ILogger<DashboardService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DashboardDTO?> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<DashboardDTO>(
                    $"api/dashboard?tenantId={tenantId}", 
                    cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data for tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}

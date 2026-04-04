using OrderCloud.Blazor.Models;

namespace OrderCloud.Blazor.Services
{
    public interface IBillService
    {
        Task<List<BillDTO>> GetAllBillsAsync();
        Task<BillDTO?> GetBillByIdAsync(Guid id);
        Task<bool> DeleteBillAsync(Guid id);
    }

    public class BillService : IBillService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BillService> _logger;

        public BillService(HttpClient httpClient, ILogger<BillService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<BillDTO>> GetAllBillsAsync()
        {
            try
            {
                var bills = await _httpClient.GetFromJsonAsync<List<BillDTO>>("api/bills");
                return bills ?? new List<BillDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bills");
                throw;
            }
        }

        public async Task<BillDTO?> GetBillByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<BillDTO>($"api/bills/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bill {BillId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteBillAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/bills/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bill {BillId}", id);
                return false;
            }
        }
    }
}

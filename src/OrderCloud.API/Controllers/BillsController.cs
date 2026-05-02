using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;
using OrderCloud.Shared.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BillsController> _logger;

        public BillsController(ApplicationDbContext db, ILogger<BillsController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            try
            {
                var bills = await _db.Bills
                    .Include(b => b.Items)
                    .Include(b => b.Tenant)
                    .AsNoTracking()
                    .OrderByDescending(b => b.BillDate)
                    .ToListAsync(cancellationToken);

                foreach (var bill in bills)
                {
                    if (bill.Items != null)
                    {
                        foreach (var item in bill.Items)
                        {
                            item.Bill = null;
                        }
                    }
                }

                return Ok(bills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bills");
                return StatusCode(500, "Error retrieving bills");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BillDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var bill = await _db.Bills
                    .Include(b => b.Items)
                    .Include(b => b.Tenant)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

                if (bill == null)
                {
                    return NotFound();
                }

                if (bill.Items != null)
                {
                    foreach (var item in bill.Items)
                    {
                        item.Bill = null;
                    }
                }

                return Ok(bill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bill {BillId}", id);
                return StatusCode(500, "Error retrieving bill");
            }
        }

        [HttpPost]
        public async Task<ActionResult<BillDTO>> Create([FromBody] BillDTO bill, CancellationToken cancellationToken = default)
        {
            if (bill == null)
            {
                return BadRequest("Bill is required.");
            }

            if (string.IsNullOrWhiteSpace(bill.PaymentMethod))
            {
                return BadRequest("Payment method is required.");
            }

            try
            {
                if (bill.Id == Guid.Empty)
                {
                    bill.Id = Guid.NewGuid();
                }

                if (bill.BillDate == default)
                {
                    bill.BillDate = DateTime.UtcNow;
                }

                bill.Tenant = null;
                bill.Items ??= new List<BillItemDTO>();

                foreach (var item in bill.Items)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.Id = Guid.NewGuid();
                    }
                    item.BillId = bill.Id;
                    item.Total = item.Price * item.Quantity;
                    item.Bill = null;
                }

                bill.Subtotal = bill.Items.Sum(i => i.Total);
                bill.Total = bill.Subtotal;

                _db.Bills.Add(bill);
                await _db.SaveChangesAsync(cancellationToken);

                return CreatedAtAction(nameof(GetById), new { id = bill.Id }, bill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bill");
                return StatusCode(500, "Error creating bill");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
                if (bill == null)
                {
                    return NotFound();
                }

                _db.Bills.Remove(bill);
                await _db.SaveChangesAsync(cancellationToken);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bill {BillId}", id);
                return StatusCode(500, "Error deleting bill");
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics(CancellationToken cancellationToken = default)
        {
            try
            {
                var bills = await _db.Bills.AsNoTracking().ToListAsync(cancellationToken);

                var statistics = new
                {
                    TotalBills = bills.Count,
                    TotalRevenue = bills.Sum(b => b.Total),
                    CashPayments = bills.Count(b => b.PaymentMethod == "Cash"),
                    CardPayments = bills.Count(b => b.PaymentMethod == "Card"),
                    TodaysBills = bills.Count(b => b.BillDate.Date == DateTime.UtcNow.Date),
                    TodaysRevenue = bills.Where(b => b.BillDate.Date == DateTime.UtcNow.Date).Sum(b => b.Total)
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics");
                return StatusCode(500, "Error retrieving statistics");
            }
        }
    }
}


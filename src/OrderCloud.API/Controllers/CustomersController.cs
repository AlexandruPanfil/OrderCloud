using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;
using OrderCloud.Shared.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ApplicationDbContext db, ILogger<CustomersController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDTO>>> GetAll(CancellationToken cancellationToken = default)
        {
            var list = await _db.Customers.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CustomerDTO>> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var customer = await _db.Customers.FindAsync(new object[] { id }, cancellationToken);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerDTO>> Create([FromBody] CustomerDTO customer, CancellationToken cancellationToken = default)
        {
            if (customer == null) return BadRequest();

            if (customer.Id == Guid.Empty) customer.Id = Guid.NewGuid();

            _db.Customers.Add(customer);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Customer in Create");
                return StatusCode(500, "Error saving customer");
            }

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CustomerDTO>> Update(Guid id, [FromBody] CustomerDTO customer, CancellationToken cancellationToken = default)
        {
            if (customer == null || id == Guid.Empty) return BadRequest();

            var existing = await _db.Customers.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return NotFound();

            existing.Name = customer.Name;
            existing.IDNO = customer.IDNO;
            //existing.PhoneNumber = customer.PhoneNumber;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Customer in Update");
                return StatusCode(500, "Error updating customer");
            }

            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Customers.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null) return NotFound();

            var hasOrders = await _db.Orders.AnyAsync(o => o.CustomerId == id, cancellationToken);
            if (hasOrders)
            {
                return BadRequest("Cannot delete customer because they have existing orders.");
            }

            _db.Customers.Remove(existing);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Customer");
                return StatusCode(500, "Error deleting customer");
            }

            return NoContent();
        }
    }
}

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OrderCloud.Blazor.Models;

namespace OrderCloud.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        // Простой потокобезопасный in-memory store — замените на репозиторий/DbContext при необходимости.
        private static readonly ConcurrentDictionary<Guid, TenantDTO> Store = new();

        private readonly ILogger<TenantsController> _logger;

        public TenantsController(ILogger<TenantsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TenantDTO>> GetAll(CancellationToken cancellationToken = default)
        {
            return Ok(Store.Values);
        }

        [HttpGet("{id:guid}")]
        public ActionResult<TenantDTO> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            if (Store.TryGetValue(id, out var tenant))
                return Ok(tenant);

            return NotFound();
        }

        [HttpPost]
        public ActionResult<TenantDTO> Create([FromBody] TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null) return BadRequest();

            if (tenant.Id == Guid.Empty) tenant.Id = Guid.NewGuid();

            // Генерируем ApiKey/ApiSecret, если их нет
            tenant.ApiKey ??= GenerateUniqueKey();
            tenant.ApiSecret ??= GenerateSecret();

            // Простая гарантия уникальности
            if (!Store.TryAdd(tenant.Id, tenant))
            {
                _logger.LogWarning("Tenant with id {Id} already exists.", tenant.Id);
                return Conflict($"Tenant with id {tenant.Id} already exists.");
            }

            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
        }

        [HttpPut("{id:guid}")]
        public ActionResult<TenantDTO> Update(Guid id, [FromBody] TenantDTO tenant, CancellationToken cancellationToken = default)
        {
            if (tenant == null) return BadRequest();
            if (id == Guid.Empty) return BadRequest();

            if (!Store.ContainsKey(id)) return NotFound();

            tenant.Id = id;
            Store[id] = tenant;
            return Ok(tenant);
        }

        [HttpDelete("{id:guid}")]
        public ActionResult Delete(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty) return BadRequest();

            if (!Store.TryRemove(id, out _)) return NotFound();

            return NoContent();
        }

        // Генерирует короткий API ключ (base64url) и обеспечивает уникальность по хранилищу.
        private static string GenerateUniqueKey(int bytes = 32)
        {
            string key;
            do
            {
                key = GenerateBase64Url(bytes);
            } while (Store.Values.Any(t => string.Equals(t.ApiKey, key, StringComparison.Ordinal)));
            return key;
        }

        // Генерирует секрет (длиннее, base64url)
        private static string GenerateSecret(int bytes = 32)
        {
            return GenerateBase64Url(bytes);
        }

        private static string GenerateBase64Url(int bytes)
        {
            Span<byte> buffer = stackalloc byte[bytes];
            RandomNumberGenerator.Fill(buffer);
            // Base64 URL-safe without padding
            var base64 = Convert.ToBase64String(buffer);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}

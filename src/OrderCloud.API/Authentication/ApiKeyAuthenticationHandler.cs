using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderCloud.Shared.Data;

namespace OrderCloud.API.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public string ApiSecretHeaderName { get; set; } = "X-API-Secret";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApplicationDbContext _db;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var apiKeyValues) ||
            !Request.Headers.TryGetValue(Options.ApiSecretHeaderName, out var apiSecretValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyValues.ToString();
        var apiSecret = apiSecretValues.ToString();

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.ApiSecret == apiSecret);

        if (tenant == null)
        {
            return AuthenticateResult.Fail("Invalid API credentials");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, tenant.Id.ToString()),
            new Claim(ClaimTypes.Name, tenant.Name),
            new Claim("TenantId", tenant.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
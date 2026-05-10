using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Data;

namespace OrderCloud.Blazor.Services;

/// <summary>
/// Adds API key authentication headers for requests from Blazor to the API.
/// Uses the tenant associated with the authenticated user.
/// </summary>
public class ApiKeyHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public ApiKeyHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Get the user's ID
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Get the first tenant associated with this user
                var tenant = await db.Tenants
                    .AsNoTracking()
                    .Where(t => t.ApplicationUsers.Any(u => u.Id == userId))
                    .FirstOrDefaultAsync(cancellationToken);

                if (tenant != null)
                {
                    // Add API key headers
                    request.Headers.Add("X-API-Key", tenant.ApiKey);
                    request.Headers.Add("X-API-Secret", tenant.ApiSecret);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

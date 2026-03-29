using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Blazor.Services
{
    public interface ITenantSelectionService
    {
        Guid? SelectedTenantId { get; }
        string? CurrentUserId { get; }
        IReadOnlyList<TenantDTO> AvailableTenants { get; }
        bool IsInitialized { get; }
        event Action? Changed;
        Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
        Task RefreshAsync(CancellationToken cancellationToken = default);
        bool CanAccessTenant(Guid tenantId);
        void SetTenant(Guid? tenantId);
    }

    public sealed class TenantSelectionService : ITenantSelectionService
    {
        private readonly ITenantService tenantService;
        private readonly AuthenticationStateProvider authenticationStateProvider;
        private List<TenantDTO> availableTenants = new();
        private HashSet<Guid> availableTenantIds = new();

        public TenantSelectionService(
            ITenantService tenantService,
            AuthenticationStateProvider authenticationStateProvider)
        {
            this.tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            this.authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
        }

        public Guid? SelectedTenantId { get; private set; }

        public string? CurrentUserId { get; private set; }

        public IReadOnlyList<TenantDTO> AvailableTenants => availableTenants;

        public bool IsInitialized { get; private set; }

        public event Action? Changed;

        public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (IsInitialized)
            {
                return;
            }

            await RefreshAsync(cancellationToken);
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tenants = await tenantService.GetAllAsync(cancellationToken);
            var filteredTenants = string.IsNullOrWhiteSpace(userId)
                ? new List<TenantDTO>()
                : tenants
                    .Where(tenant => string.Equals(tenant.ApplicationUserId, userId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(tenant => tenant.Name)
                    .ToList();

            var filteredTenantIds = filteredTenants.Select(tenant => tenant.Id).ToHashSet();
            var nextSelectedTenantId = NormalizeTenantId(SelectedTenantId, filteredTenants);

            var hasChanged = !IsInitialized
                || !string.Equals(CurrentUserId, userId, StringComparison.OrdinalIgnoreCase)
                || SelectedTenantId != nextSelectedTenantId
                || !availableTenantIds.SetEquals(filteredTenantIds)
                || HaveTenantDetailsChanged(filteredTenants);

            CurrentUserId = userId;
            availableTenants = filteredTenants;
            availableTenantIds = filteredTenantIds;
            SelectedTenantId = nextSelectedTenantId;
            IsInitialized = true;

            if (hasChanged)
            {
                Changed?.Invoke();
            }
        }

        public bool CanAccessTenant(Guid tenantId) => availableTenantIds.Contains(tenantId);

        public void SetTenant(Guid? tenantId)
        {
            var normalizedTenantId = NormalizeTenantId(tenantId, availableTenants);
            if (SelectedTenantId == normalizedTenantId)
            {
                return;
            }

            SelectedTenantId = normalizedTenantId;
            Changed?.Invoke();
        }

        private bool HaveTenantDetailsChanged(IReadOnlyList<TenantDTO> nextTenants)
        {
            if (availableTenants.Count != nextTenants.Count)
            {
                return true;
            }

            for (var i = 0; i < availableTenants.Count; i++)
            {
                var current = availableTenants[i];
                var next = nextTenants[i];

                if (current.Id != next.Id ||
                    !string.Equals(current.Name, next.Name, StringComparison.Ordinal) ||
                    !string.Equals(current.ApiKey, next.ApiKey, StringComparison.Ordinal) ||
                    !string.Equals(current.ApiSecret, next.ApiSecret, StringComparison.Ordinal) ||
                    !string.Equals(current.ApplicationUserId, next.ApplicationUserId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Guid? NormalizeTenantId(Guid? tenantId, IReadOnlyList<TenantDTO> tenants)
        {
            if (tenantId.HasValue && tenants.Any(tenant => tenant.Id == tenantId.Value))
            {
                return tenantId.Value;
            }

            return null;
        }
    }
}

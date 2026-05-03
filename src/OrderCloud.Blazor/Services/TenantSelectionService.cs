using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OrderCloud.Shared.Models;

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
        Task SetTenantAsync(Guid? tenantId);
        Task<Guid?> GetStoredTenantIdAsync();
        Task PersistTenantIdAsync(Guid? tenantId);
    }

    public sealed class TenantSelectionService : ITenantSelectionService
    {
        private const string StorageKey = "ordercloud.selected-tenant-id";
        
        private readonly ITenantService tenantService;
        private readonly AuthenticationStateProvider authenticationStateProvider;
        private readonly IJSRuntime jsRuntime;
        private List<TenantDTO> availableTenants = new();
        private HashSet<Guid> availableTenantIds = new();
        private bool hasRestoredFromStorage;

        public TenantSelectionService(
            ITenantService tenantService,
            AuthenticationStateProvider authenticationStateProvider,
            IJSRuntime jsRuntime)
        {
            this.tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            this.authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
            this.jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
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
            
            // Restore from localStorage on first initialization
            Guid? nextSelectedTenantId;
            if (!hasRestoredFromStorage && !IsInitialized)
            {
                hasRestoredFromStorage = true;
                var storedId = await GetStoredTenantIdAsync();
                nextSelectedTenantId = NormalizeTenantId(storedId, filteredTenants);
            }
            else
            {
                nextSelectedTenantId = NormalizeTenantId(SelectedTenantId, filteredTenants);
            }

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

        public async Task SetTenantAsync(Guid? tenantId)
        {
            var normalizedTenantId = NormalizeTenantId(tenantId, availableTenants);
            
            Console.WriteLine($"SetTenantAsync called: old={SelectedTenantId}, new={normalizedTenantId}");
            
            if (SelectedTenantId == normalizedTenantId)
            {
                Console.WriteLine("Tenant ID unchanged, skipping");
                return;
            }

            SelectedTenantId = normalizedTenantId;
            
            // Persist to localStorage
            try
            {
                Console.WriteLine($"Attempting to persist tenant {normalizedTenantId} to localStorage with key '{StorageKey}'");
                await PersistTenantIdAsync(normalizedTenantId);
                Console.WriteLine("Successfully persisted to localStorage");
            }
            catch (Exception ex)
            {
                // Log but don't fail if persistence fails
                Console.WriteLine($"ERROR: Failed to persist tenant selection: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Changed?.Invoke();
        }

        public async Task<Guid?> GetStoredTenantIdAsync()
        {
            try
            {
                var stored = await jsRuntime.InvokeAsync<string?>(
                    "localStorage.getItem", 
                    cancellationToken: default,
                    StorageKey);
                return Guid.TryParse(stored, out var id) ? id : null;
            }
            catch (InvalidOperationException)   // Выбрасывается в процессе prerender (до того как SignalR подключен)
            {
                return null;
            }
            catch (JSException)
            {
                return null;
            }
        }

        public async Task PersistTenantIdAsync(Guid? tenantId)
        {
            try
            {
                if (tenantId.HasValue)
                {
                    var tenantIdStr = tenantId.Value.ToString();
                    Console.WriteLine($"Calling localStorage.setItem('{StorageKey}', '{tenantIdStr}')");
                    await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, tenantIdStr);
                    Console.WriteLine("localStorage.setItem completed");
                }
                else
                {
                    Console.WriteLine($"Calling localStorage.removeItem('{StorageKey}')");
                    await jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                    Console.WriteLine("localStorage.removeItem completed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in PersistTenantIdAsync: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to see the error in the parent catch
            }
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


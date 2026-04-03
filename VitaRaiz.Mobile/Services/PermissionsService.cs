using System.Text.Json.Serialization;
using NLog;

namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Fetches and caches the current user's permissions from the API.
/// Call LoadAsync() once after login. Use Has() anywhere in the app.
/// </summary>
public class PermissionsService
{
    private readonly Logger _logger = AppLogger.Get();
    private readonly ApiService _apiService;
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public PermissionsService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public bool IsLoaded => _loaded;

    /// <summary>Loads permissions from the API. Idempotent unless forceReload=true.</summary>
    public async Task LoadAsync(bool forceReload = false)
    {
        if (_loaded && !forceReload) return;

        try
        {
            _logger.Info("[PermissionsService] Loading permissions from API...");
            var response = await _apiService.GetAsync<PermissionsResponse>("api/auth/permissions");
            _permissions.Clear();

            if (response?.Permissions != null)
            {
                foreach (var p in response.Permissions)
                    _permissions.Add(p);
                _loaded = true;
                _logger.Info("[PermissionsService] Loaded {Count} permissions", _permissions.Count);
            }
            else
            {
                _logger.Warn("[PermissionsService] Empty or null permissions response");
                // Set loaded anyway to avoid infinite retries, just with empty set
                _loaded = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[PermissionsService] Failed to load permissions");
            // Don't mark loaded so it can retry next time
        }
    }

    /// <summary>Clears cached permissions (call on logout).</summary>
    public void Clear()
    {
        _permissions.Clear();
        _loaded = false;
    }

    /// <summary>Returns true if the current user has the given permission code.</summary>
    public bool Has(string permissionCode) =>
        _permissions.Contains(permissionCode);

    // ── Convenience permission checks ────────────────────────────────

    // Sales
    public bool CanCreateSale    => Has("sales.create");
    public bool CanViewOwnSales  => Has("sales.view_own")  || Has("sales.view_all");
    public bool CanViewAllSales  => Has("sales.view_all");
    public bool CanViewTeamSales => Has("sales.view_team") || Has("sales.view_all");
    public bool CanEditOwnSale   => Has("sales.edit_own")  || Has("sales.edit_team");
    public bool CanEditTeamSale  => Has("sales.edit_team");
    public bool CanAnnulSale     => Has("sales.annul");

    // Customers
    public bool CanViewCustomers   => Has("customers.view");
    public bool CanCreateCustomer  => Has("customers.create");
    public bool CanEditCustomer    => Has("customers.edit");

    // Payments / Cobros
    public bool CanCreatePayment  => Has("payments.create");
    public bool CanViewPayments   => Has("payments.view");
    public bool CanApprovePayment => Has("payments.approve");

    // Reports
    public bool CanViewReports => Has("reports.view");
}

internal class PermissionsResponse
{
    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
}

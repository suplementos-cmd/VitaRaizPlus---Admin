using VitaRaiz.WebPortal.Models;
using NLog;

namespace VitaRaiz.WebPortal.Services;

/// <summary>Mirrors Mobile PermissionsService — fetches and caches permissions from the API.</summary>
public class PermissionsService
{
    private readonly ApiService _api;
    private readonly HashSet<string> _perms = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    public PermissionsService(ApiService api) => _api = api;

    public bool IsLoaded => _loaded;

    public async Task LoadAsync(bool force = false)
    {
        if (_loaded && !force) return;
        try
        {
            var resp = await _api.GetAsync<PermissionsResponse>("api/auth/permissions");
            _perms.Clear();
            if (resp?.Permissions is { Count: > 0 })
                foreach (var p in resp.Permissions) _perms.Add(p);
            _loaded = true;
            _log.Info("[Perms] Loaded {Count} permissions", _perms.Count);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[Perms] Load failed");
        }
    }

    public void Clear() { _perms.Clear(); _loaded = false; }

    public bool Has(string code) => _perms.Contains(code);

    // Sales
    public bool CanCreateSale   => Has("sales.create");
    public bool CanViewOwnSales => Has("sales.view_own") || Has("sales.view_all");
    public bool CanViewAllSales => Has("sales.view_all");
    public bool CanAnnulSale    => Has("sales.annul");

    // Customers
    public bool CanViewCustomers  => Has("customers.view");
    public bool CanCreateCustomer => Has("customers.create");
    public bool CanEditCustomer   => Has("customers.edit");

    // Payments
    public bool CanCreatePayment  => Has("payments.create");
    public bool CanViewPayments   => Has("payments.view");
    public bool CanApprovePayment => Has("payments.approve");

    // Reports / Admin
    public bool CanViewReports  => Has("reports.view");
    public bool CanManageUsers  => Has("admin.users");
    public bool CanManageRoles  => Has("admin.roles");
    public bool CanManageCatalogs => Has("admin.catalogs");
}

using VitaRaiz.Mobile.Models;
using NLog;

namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Caches catalogs from the API and resolves status labels/colors dynamically.
/// Call LoadAsync() once at startup after login.
/// </summary>
public class CatalogService
{
    private readonly Logger _logger = AppLogger.Get();
    private readonly ApiService _apiService;

    private List<CatalogSaleStatus> _saleStatuses = new();
    private List<CatalogPaymentStatus> _paymentStatuses = new();
    private List<CatalogRiskStatus> _riskStatuses = new();
    private List<CatalogVisitAction> _visitActions = new();
    private CatalogAppTheme? _theme;
    private bool _loaded;

    public CatalogService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public IReadOnlyList<CatalogSaleStatus> SaleStatuses => _saleStatuses;
    public IReadOnlyList<CatalogPaymentStatus> PaymentStatuses => _paymentStatuses;
    public IReadOnlyList<CatalogRiskStatus> RiskStatuses => _riskStatuses;
    public IReadOnlyList<CatalogVisitAction> VisitActions => _visitActions;
    public CatalogAppTheme? Theme => _theme;
    public bool IsLoaded => _loaded;

    public async Task LoadAsync()
    {
        if (_loaded)
        {
            _logger.Debug("[LoadAsync] Catalogs already loaded, skipping");
            return;
        }
            
        _logger.Info("[LoadAsync] Loading catalogs from API...");
        try
        {
            var data = await _apiService.GetAsync<AllCatalogsResponse>("api/catalogs/all");
            if (data != null)
            {
                _saleStatuses = data.SaleStatuses;
                _paymentStatuses = data.PaymentStatuses;
                _riskStatuses = data.RiskStatuses;
                _visitActions = data.VisitActions;
                _theme = data.Theme;
                _loaded = true;
                
                _logger.Info("[LoadAsync] Catalogs loaded successfully: SaleStatuses={SaleCount}, PaymentStatuses={PaymentCount}, RiskStatuses={RiskCount}",
                    _saleStatuses.Count, _paymentStatuses.Count, _riskStatuses.Count);
            }
            else
            {
                _logger.Warn("[LoadAsync] API returned null, using defaults");
                EnsureDefaults();
                _loaded = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[LoadAsync] Error loading catalogs, using defaults");
            System.Diagnostics.Debug.WriteLine($"[CatalogService] Error loading catalogs: {ex.Message}");
            EnsureDefaults();
            _loaded = true;
        }
    }

    /// <summary>
    /// Resolve sale status label and color from the catalog.
    /// Normalized status codes: active, pending, completed, cancelled.
    /// </summary>
    public (string label, string color, string? icon) ResolveSaleStatus(string statusCode)
    {
        var match = _saleStatuses.FirstOrDefault(s =>
            s.StatusCode.Equals(statusCode, StringComparison.OrdinalIgnoreCase));

        if (match != null)
            return (match.StatusName, match.ColorHex, match.Icon);

        // Fallback for common normalized codes
        return statusCode?.ToLower() switch
        {
            "active" or "en_proceso" => ("En Proceso", "#28A745", "🟢"),
            "pending" or "por_iniciar" => ("Por Iniciar", "#FFC107", "🟡"),
            "completed" or "liquidado" => ("Liquidada", "#17A2B8", "🔵"),
            "cancelled" or "cancelado" => ("Cancelada", "#DC3545", "🔴"),
            _ => (statusCode ?? "Desconocido", "#999999", null)
        };
    }

    /// <summary>
    /// Resolve payment status label and color from the catalog.
    /// </summary>
    public (string label, string color, string? icon) ResolvePaymentStatus(string statusCode)
    {
        var match = _paymentStatuses.FirstOrDefault(s =>
            s.StatusCode.Equals(statusCode, StringComparison.OrdinalIgnoreCase));

        if (match != null)
            return (match.StatusName, match.ColorHex, match.Icon);

        return statusCode?.ToLower() switch
        {
            "pending" => ("Pendiente", "#FFC107", "🟡"),
            "approved" => ("Aprobado", "#28A745", "🟢"),
            "rejected" => ("Rechazado", "#DC3545", "🔴"),
            "cancelled" => ("Cancelado", "#6C757D", "⚫"),
            _ => (statusCode ?? "Desconocido", "#999999", null)
        };
    }

    /// <summary>
    /// Resolve risk status color/label from the catalog.
    /// Risk codes: VERDE, AMARILLO, ROJO, CRITICO.
    /// </summary>
    public (string label, string color, string? icon) ResolveRiskStatus(string riskCode)
    {
        var match = _riskStatuses.FirstOrDefault(s =>
            s.StatusCode.Equals(riskCode, StringComparison.OrdinalIgnoreCase));

        if (match != null)
            return (match.StatusName, match.ColorHex, match.Icon);

        return riskCode?.ToUpper() switch
        {
            "VERDE" => ("Al corriente", "#28A745", "🟢"),
            "AMARILLO" => ("Atención", "#FFC107", "🟡"),
            "ROJO" => ("Alerta", "#DC3545", "🔴"),
            "CRITICO" => ("Crítico", "#8B0000", "🔴"),
            _ => (riskCode ?? "Desconocido", "#999999", null)
        };
    }

    /// <summary>
    /// Get visit actions filtered by role context.
    /// </summary>
    public List<CatalogVisitAction> GetActionsForRole(string role)
    {
        // All actions are available, UI decides which to show per role
        return _visitActions.ToList();
    }

    private void EnsureDefaults()
    {
        if (_saleStatuses.Count == 0)
        {
            _saleStatuses = new List<CatalogSaleStatus>
            {
                new() { StatusCode = "active", StatusName = "En Proceso", ColorHex = "#28A745", Icon = "🟢", DisplayOrder = 1 },
                new() { StatusCode = "pending", StatusName = "Por Iniciar", ColorHex = "#FFC107", Icon = "🟡", DisplayOrder = 2 },
                new() { StatusCode = "completed", StatusName = "Liquidada", ColorHex = "#17A2B8", Icon = "🔵", DisplayOrder = 3 },
                new() { StatusCode = "cancelled", StatusName = "Cancelada", ColorHex = "#DC3545", Icon = "🔴", DisplayOrder = 4 }
            };
        }
        if (_paymentStatuses.Count == 0)
        {
            _paymentStatuses = new List<CatalogPaymentStatus>
            {
                new() { StatusCode = "pending", StatusName = "Pendiente", ColorHex = "#FFC107", Icon = "🟡", DisplayOrder = 1 },
                new() { StatusCode = "approved", StatusName = "Aprobado", ColorHex = "#28A745", Icon = "🟢", DisplayOrder = 2 },
                new() { StatusCode = "rejected", StatusName = "Rechazado", ColorHex = "#DC3545", Icon = "🔴", DisplayOrder = 3 }
            };
        }
        if (_riskStatuses.Count == 0)
        {
            _riskStatuses = new List<CatalogRiskStatus>
            {
                new() { StatusCode = "VERDE", StatusName = "Al corriente", ColorHex = "#28A745", MinDays = 0, MaxDays = 7 },
                new() { StatusCode = "AMARILLO", StatusName = "Atención", ColorHex = "#FFC107", MinDays = 8, MaxDays = 14 },
                new() { StatusCode = "ROJO", StatusName = "Alerta", ColorHex = "#DC3545", MinDays = 15, MaxDays = 21 },
                new() { StatusCode = "CRITICO", StatusName = "Crítico", ColorHex = "#8B0000", MinDays = 22, MaxDays = 999 }
            };
        }
    }
}

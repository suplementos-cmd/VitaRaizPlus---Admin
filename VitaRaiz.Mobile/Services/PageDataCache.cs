namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Transfers large objects between pages without URL serialization.
/// Set before navigating; consuming page clears it immediately after reading.
/// </summary>
public static class PageDataCache
{
    /// <summary>Sale data to pre-populate the edit form. Cleared after consumed.</summary>
    public static Models.SaleFullDetail? PendingEditSale { get; set; }

    /// <summary>
    /// When set, SalesPage will force a list refresh on its next OnAppearing
    /// instead of skipping due to the _initialized guard.
    /// </summary>
    public static bool ForceRefreshSalesList { get; set; }

    /// <summary>
    /// When set, SaleDetailPage will use this fresh data on its next OnAppearing
    /// instead of making a new API call. Cleared after consumed.
    /// SaleId = -1 means a new sale was created (no detail to refresh).
    /// </summary>
    public static Models.SaleFullDetail? RefreshedSaleDetail { get; set; }

    // ── Background prefetch — populated by LoginPage during authentication ────────
    /// <summary>
    /// Raw sales list pre-fetched during login. SalesPage consumes this on first
    /// OnAppearing and sets it to null. Falls back to API call if null.
    /// </summary>
    public static List<SaleDto>? PrefetchedSales { get; set; }

    /// <summary>
    /// Raw customers list pre-fetched during login. CustomersPage consumes this on
    /// first load and sets it to null. Falls back to API call if null.
    /// </summary>
    public static List<CustomerDto>? PrefetchedCustomers { get; set; }
}

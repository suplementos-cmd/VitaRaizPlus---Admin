namespace VitaRaiz.Domain.Constants;

/// <summary>
/// IDs numéricos de CATALOG_PAYMENT_STATUSES (TYPE = WORKFLOW).
/// Asignados fijos en migración 07b — usar solo para filtros EF Core en memoria.
/// Las cadenas de texto (StatusCode, StatusName) se obtienen siempre del catálogo
/// mediante ICatalogRepository.GetPaymentStatusesAsync().
/// </summary>
public static class PaymentStatusCodes
{
    public const int Pending    = 1;
    public const int Confirmado = 2;
    public const int Rechazado  = 3;
    public const int Cancelled  = 4;
}

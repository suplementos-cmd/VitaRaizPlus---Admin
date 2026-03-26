using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;
using System.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class SaleRepository : BaseOracleRepository, ISaleRepository
{
    public SaleRepository(VitaRaizDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Normaliza el status de Oracle a un valor consistente en inglés para los clientes.
    /// Oracle usa: EN_PROCESO, POR_INICIAR, LIQUIDADO, CANCELADO
    /// API devuelve: active, pending, completed, cancelled
    /// </summary>
    private static string NormalizeStatus(string? oracleStatus)
    {
        return (oracleStatus?.ToUpper()) switch
        {
            "EN_PROCESO" or "ACTIVE" => "active",
            "POR_INICIAR" or "PENDING" => "pending",
            "LIQUIDADO" or "COMPLETED" => "completed",
            "CANCELADO" or "CANCELLED" => "cancelled",
            _ => oracleStatus?.ToLower() ?? "unknown"
        };
    }

    public async Task<int> CreateSaleAsync(int customerId, int sellerId, int paymentTermDays, 
        string? notes, List<SaleDetailDto> details)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_sale");

        var saleIdParam = AddOutputParameter(command, "p_sale_id");

        AddInputParameter(command, "p_customer_id", customerId);
        AddInputParameter(command, "p_seller_id", sellerId);
        AddInputParameter(command, "p_payment_term_days", paymentTermDays);
        AddInputParameter(command, "p_notes", notes);

        await command.ExecuteNonQueryAsync();

        int saleId = GetOutputValue((OracleParameter)saleIdParam);

        // Agregar detalles de venta
        foreach (var detail in details)
        {
            using var detailCommand = CreatePackageProcedureCommand(connection, "sp_add_sale_detail");

            AddInputParameter(detailCommand, "p_sale_id", saleId);
            AddInputParameter(detailCommand, "p_product_id", detail.ProductId);
            AddInputParameter(detailCommand, "p_quantity", detail.Quantity);
            AddInputParameter(detailCommand, "p_unit_price", detail.UnitPrice);

            await detailCommand.ExecuteNonQueryAsync();
        }

        return saleId;
    }

    public async Task<bool> CancelSaleAsync(int saleId, string? reason)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_cancel_sale");

        AddInputParameter(command, "p_sale_id", saleId);
        AddInputParameter(command, "p_user_id", DBNull.Value);
        AddInputParameter(command, "p_reason", reason);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<SaleDto>> GetSalesAsync(int? customerId, int? sellerId, 
        DateTime? startDate, DateTime? endDate, string? status)
    {
        try
        {
            Console.WriteLine($"[SaleRepository] GetSalesAsync - Params: customerId={customerId}, sellerId={sellerId}, status={status}");
            
            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Seller)
                .Include(s => s.Payments) // Necesario para calcular PaidAmount
                .AsQueryable();

            if (customerId.HasValue)
                query = query.Where(s => s.CustomerId == customerId.Value);

            if (sellerId.HasValue)
                query = query.Where(s => s.SellerId == sellerId.Value);

            if (startDate.HasValue)
                query = query.Where(s => s.SaleDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SaleDate <= endDate.Value);

            if (!string.IsNullOrEmpty(status))
            {
                // Mapear estados de inglés a Oracle
                var oracleStatus = status.ToLower() switch
                {
                    "active" => "EN_PROCESO",
                    "pending" => "POR_INICIAR",
                    "completed" => "LIQUIDADO",
                    "cancelled" => "CANCELADO",
                    _ => status.ToUpper()
                };
                
                query = query.Where(s => s.Status.ToUpper() == oracleStatus);
                Console.WriteLine($"[SaleRepository] Filtro status: '{status}' -> '{oracleStatus}'");
            }

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new SaleDto
                {
                    SaleId = s.SaleId,
                    CustomerName = s.Customer != null ? s.Customer.CustomerName : "Sin cliente",
                    TotalAmount = s.TotalAmount,
                    PaidAmount = s.Payments.Where(p => p.Status.ToUpper() == "APPROVED").Sum(p => p.Amount),
                    Balance = s.TotalAmount - s.Payments.Where(p => p.Status.ToUpper() == "APPROVED").Sum(p => p.Amount),
                    SaleDate = s.SaleDate,
                    Status = s.Status, // Se normaliza después de materializar
                    PaymentTerms = $"{s.PaymentTermDays} días"
                })
                .ToListAsync();

            // Normalizar status de Oracle a inglés después de materializar la query
            foreach (var sale in sales)
                sale.Status = NormalizeStatus(sale.Status);

            Console.WriteLine($"[SaleRepository] Devolviendo {sales.Count} ventas");
            return sales;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaleRepository] ERROR: {ex.Message}");
            throw;
        }
    }

    public async Task<List<SaleDto>> GetActiveSalesAsync(int? collectorId)
    {
        try
        {
            Console.WriteLine($"[SaleRepository] GetActiveSalesAsync - collectorId={collectorId}");
            
            // Ventas activas: buscar EN_PROCESO (Oracle status) o active (legacy)
            // No podemos filtrar por PaidAmount en el Where porque no existe la columna - filtraremos después
            var query = _context.Sales
                .Include(s => s.Customer)
                .ThenInclude(c => c!.Zone)
            .Include(s => s.Payments)
            .Where(s => s.Status != null && (s.Status.ToUpper() == "EN_PROCESO" || s.Status.ToUpper() == "ACTIVE" || s.Status.ToUpper() == "POR_INICIAR"));

            if (collectorId.HasValue)
            {
                query = query.Where(s => s.Customer != null && s.Customer.ZoneId != null);
            }

            var salesData = await query
                .OrderBy(s => s.SaleDate)
                .ToListAsync();
            
            Console.WriteLine($"[SaleRepository] Query ejecutado, procesando {salesData.Count} ventas");
            
            // Calcular PaidAmount y filtrar las que tienen saldo pendiente
            var sales = salesData
                .Select(s => 
                {
                    var paidAmount = (s.Payments ?? new List<Payment>())
                        .Where(p => p.Status != null && p.Status.ToUpper() == "APPROVED")
                        .Sum(p => p.Amount);
                    return new SaleDto
                    {
                        SaleId = s.SaleId,
                        CustomerName = s.Customer?.CustomerName ?? "Sin cliente",
                        TotalAmount = s.TotalAmount,
                        PaidAmount = paidAmount,
                        Balance = s.TotalAmount - paidAmount,
                        SaleDate = s.SaleDate,
                        Status = NormalizeStatus(s.Status),
                        PaymentTerms = $"{s.PaymentTermDays} días"
                    };
                })
                .Where(s => s.Balance > 0) // Solo ventas con saldo pendiente
                .ToList();

            Console.WriteLine($"[SaleRepository] Devolviendo {sales.Count} ventas activas");
            return sales;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaleRepository] ERROR: {ex.Message}");
            Console.WriteLine($"[SaleRepository] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<SaleDto?> GetSaleByIdAsync(int saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Seller)
            .Include(s => s.Payments)
            .Where(s => s.SaleId == saleId)
            .FirstOrDefaultAsync();
        
        if (sale == null)
            return null;
        
        var paidAmount = (sale.Payments ?? new List<Payment>())
            .Where(p => p.Status != null && p.Status.ToUpper() == "APPROVED").Sum(p => p.Amount);
        
        return new SaleDto
        {
            SaleId = sale.SaleId,
            CustomerName = sale.Customer?.CustomerName ?? "Sin cliente",
            TotalAmount = sale.TotalAmount,
            PaidAmount = paidAmount,
            Balance = sale.TotalAmount - paidAmount,
            SaleDate = sale.SaleDate,
            Status = NormalizeStatus(sale.Status),
            PaymentTerms = $"{sale.PaymentTermDays} días"
        };
    }

    public async Task<Sale?> GetByIdAsync(int saleId)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Seller)
            .Include(s => s.SaleDetails)
            .ThenInclude(sd => sd.Product)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);
    }

    public async Task<decimal> GetSaleBalanceAsync(int saleId)
    {
        var connection = await GetOpenConnectionAsync();
        var parameters = new Dictionary<string, object> { { "p_sale_id", saleId } };
        
        using var command = CreatePackageFunctionCommand(connection, "fn_get_sale_balance", parameters);
        await command.ExecuteNonQueryAsync();
        
        var resultParam = (OracleParameter)command.Parameters["result"];
        var resultValue = resultParam.Value;
        
        if (resultValue == null || resultValue == DBNull.Value)
            return 0;
            
        return Convert.ToDecimal(((Oracle.ManagedDataAccess.Types.OracleDecimal)resultValue).Value);
    }

    public async Task<string> GetSaleRiskStatusAsync(int saleId)
    {
        var connection = await GetOpenConnectionAsync();
        var parameters = new Dictionary<string, object> { { "p_sale_id", saleId } };
        
        using var command = CreatePackageStringFunctionCommand(connection, "fn_get_risk_status", parameters);
        await command.ExecuteNonQueryAsync();
        
        var resultParam = (OracleParameter)command.Parameters["result"];
        var resultValue = resultParam.Value;
        
        if (resultValue == null || resultValue == DBNull.Value)
            return "DESCONOCIDO";
            
        return resultValue.ToString() ?? "DESCONOCIDO";
    }

    public async Task<SaleFullDto?> GetSaleFullAsync(int saleId)
    {
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Customer).ThenInclude(c => c!.Zone)
                .Include(s => s.Seller)
                .Include(s => s.AssignedCollector)
                .Include(s => s.SaleDetails).ThenInclude(d => d.Product)
                .Include(s => s.Payments).ThenInclude(p => p.Collector)
                .Where(s => s.SaleId == saleId)
                .FirstOrDefaultAsync();

            if (sale == null) return null;

            var approvedPaid = (sale.Payments ?? new List<Payment>())
                .Where(p => p.Status != null && p.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);
            var balance = sale.TotalAmount - approvedPaid;

            // get risk + metrics from Oracle functions (best-effort)
            string riskStatus = "VERDE";
            decimal paymentPct = 0;
            int daysSince = 0;
            try
            {
                riskStatus = await GetSaleRiskStatusAsync(saleId);
                paymentPct = sale.TotalAmount > 0 ? Math.Round(approvedPaid / sale.TotalAmount * 100, 2) : 0;
                var lastPaymentDate = (sale.Payments ?? new List<Payment>())
                    .Where(p => p.Status != null && p.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => (DateTime?)p.PaymentDate)
                    .FirstOrDefault();
                daysSince = lastPaymentDate.HasValue ? (int)(DateTime.Now - lastPaymentDate.Value).TotalDays : 0;
            }
            catch { /* metrics are informational, don't fail the whole call */ }

            return new SaleFullDto
            {
                SaleId = sale.SaleId,
                TotalAmount = sale.TotalAmount,
                PaidAmount = approvedPaid,
                Balance = balance,
                SaleDate = sale.SaleDate,
                Status = NormalizeStatus(sale.Status),
                PaymentTerms = sale.PaymentTerms,
                Notes = sale.Notes,

                CustomerId = sale.CustomerId,
                CustomerName = sale.Customer?.CustomerName ?? "N/A",
                CustomerPhone = sale.Customer?.Phone,
                CustomerAddress = sale.Customer?.Address,
                CustomerGpsLatitude = sale.Customer?.GpsLatitude,
                CustomerGpsLongitude = sale.Customer?.GpsLongitude,
                CustomerIsGold = sale.Customer?.IsGoldCustomer ?? false,
                CustomerIsBlacklisted = sale.Customer?.IsBlacklisted ?? false,

                SellerId = sale.SellerId,
                SellerName = sale.Seller?.Username,
                AssignedCollectorId = sale.AssignedCollectorId,
                CollectorName = sale.AssignedCollector?.Username,

                RiskStatus = riskStatus,
                PaymentPercentage = paymentPct,
                DaysSinceLastPayment = daysSince,

                Items = (sale.SaleDetails ?? new List<SaleDetail>()).Select(d => new SaleDetailDto
                {
                    SaleDetailId = d.DetailId,
                    SaleId = d.SaleId,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.ProductName ?? "N/A",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal
                }).ToList(),

                Payments = (sale.Payments ?? new List<Payment>()).Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    SaleId = p.SaleId,
                    CollectorId = p.CollectorId,
                    CollectorName = p.Collector?.Username,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    GpsLatitude = p.GpsLatitude,
                    GpsLongitude = p.GpsLongitude,
                    Status = p.Status?.ToLower() ?? "unknown",
                    Notes = p.Notes
                }).OrderByDescending(p => p.PaymentDate).ToList()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaleRepository] ERROR in GetSaleFullAsync: {ex.Message}");
            throw;
        }
    }
}

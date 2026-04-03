using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Constants;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;
using System.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class SaleRepository : BaseOracleRepository, ISaleRepository
{
    private readonly ICatalogRepository _catalogRepository;

    public SaleRepository(VitaRaizDbContext context, ICatalogRepository catalogRepository) : base(context)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<int> CreateSaleAsync(int customerId, int sellerId, int paymentTermDays, 
        string? paymentTerm, string? collectionDay, DateTime? firstCollectionDate, decimal downPayment,
        string? notes, List<SaleDetailDto> details)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_sale");

        var saleIdParam = AddOutputParameter(command, "p_sale_id");

        AddInputParameter(command, "p_customer_id", customerId);
        AddInputParameter(command, "p_seller_id", sellerId);
        AddInputParameter(command, "p_payment_term_days", paymentTermDays);
        AddInputParameter(command, "p_payment_term", paymentTerm ?? "SEMANAL");
        AddInputParameter(command, "p_collection_day", !string.IsNullOrEmpty(collectionDay) ? collectionDay : DBNull.Value);
        AddInputParameter(command, "p_first_collection_date", firstCollectionDate.HasValue ? firstCollectionDate.Value : DBNull.Value);
        AddInputParameter(command, "p_down_payment", downPayment);
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

    public async Task<bool> UpdateSaleAsync(int saleId, string? paymentTerm, string? collectionDay,
        DateTime? firstCollectionDate, decimal downPayment, string? notes, string? status,
        DateTime saleDate)
    {
        var sale = await _context.Sales.FindAsync(saleId);
        if (sale == null) return false;

        sale.PaymentTerm = paymentTerm;
        sale.CollectionDay = collectionDay;
        sale.FirstCollectionDate = firstCollectionDate;
        sale.DownPayment = downPayment;
        sale.Notes = notes;
        if (!string.IsNullOrEmpty(status))
            sale.Status = status;
        sale.SaleDate = saleDate;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SaleDto>> GetSalesAsync(int? customerId, int? sellerId, 
        DateTime? startDate, DateTime? endDate, string? status)
    {
        try
        {
            Console.WriteLine($"[SaleRepository] GetSalesAsync - Params: customerId={customerId}, sellerId={sellerId}, status={status}");

            // Load catalog — source of truth for status codes
            var saleStatuses = await _catalogRepository.GetSaleStatusesAsync();
            var statusByCode = saleStatuses.ToDictionary(s => s.StatusCode.ToUpper(), s => s.StatusCode.ToLower());

            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Seller)
                .Include(s => s.Payments) // Necesario para calcular PaidAmount
                .Include(s => s.SaleDetails).ThenInclude(si => si.Product) // Incluir items y productos
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
                // Match catalog StatusCode (case-insensitive)—no hardcoded mapping
                var matched = saleStatuses.FirstOrDefault(s =>
                    string.Equals(s.StatusCode, status, StringComparison.OrdinalIgnoreCase));
                var oracleStatus = matched?.StatusCode.ToUpper() ?? status.ToUpper();
                query = query.Where(s => s.Status.ToUpper() == oracleStatus);
                Console.WriteLine($"[SaleRepository] Filtro status: '{status}' -> '{oracleStatus}'");
            }

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new
                {
                    SaleData = s,
                    ApprovedPayments = s.Payments.Where(p => p.StatusId == PaymentStatusCodes.Confirmado).ToList()
                })
                .ToListAsync();

            // Mapear a DTO después de materializar para evitar recalcular en cada proyección
            var result = sales.Select(item =>
            {
                var paidAmount = item.ApprovedPayments.Sum(p => p.Amount);
                var firstPayment = item.ApprovedPayments
                    .OrderBy(p => p.PaymentDate)
                    .FirstOrDefault();

                return new SaleDto
                {
                    SaleId = item.SaleData.SaleId,
                    CustomerName = item.SaleData.Customer != null ? item.SaleData.Customer.CustomerName : "Sin cliente",
                    CustomerAddress = item.SaleData.Customer != null ? item.SaleData.Customer.Address : null,
                    TotalAmount = item.SaleData.TotalAmount,
                    PaidAmount = paidAmount,
                    Balance = item.SaleData.TotalAmount - paidAmount,
                    SaleDate = item.SaleData.SaleDate,
                    FirstPaymentDate = firstPayment?.PaymentDate,
                    Status = statusByCode.TryGetValue(item.SaleData.Status?.ToUpper() ?? "", out var sc1) ? sc1 : item.SaleData.Status?.ToLower() ?? "unknown",
                    PaymentTerms = item.SaleData.PaymentTerms,
                    ProductName = item.SaleData.SaleDetails != null && item.SaleData.SaleDetails.Any() 
                        ? item.SaleData.SaleDetails.OrderBy(si => si.DetailId).First().Product!.ProductName 
                        : null,
                    SellerName = item.SaleData.Seller != null ? item.SaleData.Seller.Username : null
                };
            }).ToList();

            Console.WriteLine($"[SaleRepository] Devolviendo {result.Count} ventas");
            return result;
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

            // Load catalog for status resolution
            var saleStatuses = await _catalogRepository.GetSaleStatusesAsync();
            var statusByCode = saleStatuses.ToDictionary(s => s.StatusCode.ToUpper(), s => s.StatusCode.ToLower());

            // Active-for-collection = all catalog statuses except terminal ones (LIQUIDADO/CANCELADO)
            var activeCodes = saleStatuses
                .Where(s => !string.Equals(s.StatusCode, "LIQUIDADO", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(s.StatusCode, "CANCELADO", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.StatusCode.ToUpper())
                .ToHashSet();

            var query = _context.Sales
                .Include(s => s.Customer)
                    .ThenInclude(c => c!.Zone)
                .Include(s => s.Payments)
                .Include(s => s.Seller)
                .Include(s => s.SaleDetails).ThenInclude(si => si.Product)
                .Where(s => s.Status != null && activeCodes.Contains(s.Status.ToUpper()));

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
                        .Where(p => p.StatusId == PaymentStatusCodes.Confirmado)
                        .Sum(p => p.Amount);
                    var firstPayment = (s.Payments ?? new List<Payment>())
                        .Where(p => p.StatusId == PaymentStatusCodes.Confirmado)
                        .OrderBy(p => p.PaymentDate)
                        .FirstOrDefault();
                    return new SaleDto
                    {
                        SaleId = s.SaleId,
                        CustomerName = s.Customer?.CustomerName ?? "Sin cliente",
                        CustomerAddress = s.Customer?.Address,
                        TotalAmount = s.TotalAmount,
                        PaidAmount = paidAmount,
                        Balance = s.TotalAmount - paidAmount,
                        SaleDate = s.SaleDate,
                        FirstPaymentDate = firstPayment?.PaymentDate,
                        Status = statusByCode.TryGetValue(s.Status?.ToUpper() ?? "", out var sc2) ? sc2 : s.Status?.ToLower() ?? "unknown",
                        PaymentTerms = s.PaymentTerms,
                        ProductName = s.SaleDetails != null && s.SaleDetails.Any()
                            ? s.SaleDetails.OrderBy(si => si.DetailId).First().Product?.ProductName
                            : null,
                        SellerName = s.Seller?.Username
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
            .Include(s => s.SaleDetails).ThenInclude(si => si.Product)
            .Where(s => s.SaleId == saleId)
            .FirstOrDefaultAsync();
        
        if (sale == null)
            return null;
        
        var paidAmount = (sale.Payments ?? new List<Payment>())
            .Where(p => p.StatusId == PaymentStatusCodes.Confirmado).Sum(p => p.Amount);
        
        var firstPayment = (sale.Payments ?? new List<Payment>())
            .Where(p => p.StatusId == PaymentStatusCodes.Confirmado)
            .OrderBy(p => p.PaymentDate)
            .FirstOrDefault();

        var saleStatuses = await _catalogRepository.GetSaleStatusesAsync();
        var statusByCode = saleStatuses.ToDictionary(s => s.StatusCode.ToUpper(), s => s.StatusCode.ToLower());
        
        return new SaleDto
        {
            SaleId = sale.SaleId,
            CustomerName = sale.Customer?.CustomerName ?? "Sin cliente",
            CustomerAddress = sale.Customer?.Address,
            TotalAmount = sale.TotalAmount,
            PaidAmount = paidAmount,
            Balance = sale.TotalAmount - paidAmount,
            SaleDate = sale.SaleDate,
            FirstPaymentDate = firstPayment?.PaymentDate,
            Status = statusByCode.TryGetValue(sale.Status?.ToUpper() ?? "", out var sc) ? sc : sale.Status?.ToLower() ?? "unknown",
            PaymentTerms = sale.PaymentTerms,
            ProductName = sale.SaleDetails != null && sale.SaleDetails.Any()
                ? sale.SaleDetails.OrderBy(si => si.DetailId).First().Product?.ProductName
                : null,
            SellerName = sale.Seller?.Username
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

            // Load catalogs for status resolution
            var saleStatuses = await _catalogRepository.GetSaleStatusesAsync();
            var saleStatusByCode = saleStatuses.ToDictionary(s => s.StatusCode.ToUpper(), s => s.StatusCode.ToLower());

            var paymentStatuses = await _catalogRepository.GetPaymentStatusesAsync();
            var payStatusById = paymentStatuses.ToDictionary(s => s.StatusId, s => s.StatusCode.ToLower());

            var approvedPaid = (sale.Payments ?? new List<Payment>())
                .Where(p => p.StatusId == PaymentStatusCodes.Confirmado)
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
                    .Where(p => p.StatusId == PaymentStatusCodes.Confirmado)
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
                Status = saleStatusByCode.TryGetValue(sale.Status?.ToUpper() ?? "", out var ssc) ? ssc : sale.Status?.ToLower() ?? "unknown",
                
                // Structured payment fields
                PaymentTerm = sale.PaymentTerm,
                CollectionDay = sale.CollectionDay,
                FirstCollectionDate = sale.FirstCollectionDate,
                DownPayment = sale.DownPayment,
                
                // Legacy/Additional
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
                ZoneName = sale.Customer?.Zone?.ZoneName,

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
                    Status = payStatusById.TryGetValue(p.StatusId, out var pCode) ? pCode : p.StatusId.ToString(),
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

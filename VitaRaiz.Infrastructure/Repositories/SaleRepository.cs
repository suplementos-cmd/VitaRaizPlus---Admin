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
        var query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Seller)
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
            query = query.Where(s => s.Status.ToLower() == status.ToLower());

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleDto
            {
                SaleId = s.SaleId,
                CustomerName = s.Customer.CustomerName,
                TotalAmount = s.TotalAmount,
                PaidAmount = s.PaidAmount,
                Balance = s.TotalAmount - s.PaidAmount,
                SaleDate = s.SaleDate,
                Status = s.Status,
                PaymentTerms = $"{s.PaymentTermDays} días"
            })
            .ToListAsync();

        return sales;
    }

    public async Task<List<SaleDto>> GetActiveSalesAsync(int? collectorId)
    {
        var query = _context.Sales
            .Include(s => s.Customer)
            .Where(s => s.Status == "active" && s.TotalAmount > s.PaidAmount);

        if (collectorId.HasValue)
        {
            // Filtrar por zona del cobrador si es necesario
            query = query.Where(s => s.Customer.ZoneId != null);
        }

        var sales = await query
            .OrderBy(s => s.SaleDate)
            .Select(s => new SaleDto
            {
                SaleId = s.SaleId,
                CustomerName = s.Customer.CustomerName,
                TotalAmount = s.TotalAmount,
                PaidAmount = s.PaidAmount,
                Balance = s.TotalAmount - s.PaidAmount,
                SaleDate = s.SaleDate,
                Status = s.Status,
                PaymentTerms = $"{s.PaymentTermDays} días"
            })
            .ToListAsync();

        return sales;
    }

    public async Task<SaleDto?> GetSaleByIdAsync(int saleId)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Seller)
            .Where(s => s.SaleId == saleId)
            .Select(s => new SaleDto
            {
                SaleId = s.SaleId,
                CustomerName = s.Customer.CustomerName,
                TotalAmount = s.TotalAmount,
                PaidAmount = s.PaidAmount,
                Balance = s.TotalAmount - s.PaidAmount,
                SaleDate = s.SaleDate,
                Status = s.Status,
                PaymentTerms = $"{s.PaymentTermDays} días"
            })
            .FirstOrDefaultAsync();
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
}

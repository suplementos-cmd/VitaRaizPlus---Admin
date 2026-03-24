using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly VitaRaizDbContext _context;

    public SaleRepository(VitaRaizDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateSaleAsync(int customerId, int sellerId, int paymentTermDays, 
        string? notes, List<SaleDetailDto> details)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_register_sale";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        var saleIdParam = new OracleParameter("p_sale_id", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(saleIdParam);

        command.Parameters.Add(new OracleParameter("p_customer_id", customerId));
        command.Parameters.Add(new OracleParameter("p_seller_id", sellerId));
        command.Parameters.Add(new OracleParameter("p_payment_term_days", paymentTermDays));
        command.Parameters.Add(new OracleParameter("p_notes", notes ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        int saleId = Convert.ToInt32(((OracleDecimal)saleIdParam.Value).ToInt32());

        // Agregar detalles de venta
        foreach (var detail in details)
        {
            using var detailCommand = connection.CreateCommand();
            detailCommand.CommandText = "EM_VITARAIZ_AD.sp_add_sale_detail";
            detailCommand.CommandType = System.Data.CommandType.StoredProcedure;

            detailCommand.Parameters.Add(new OracleParameter("p_sale_id", saleId));
            detailCommand.Parameters.Add(new OracleParameter("p_product_id", detail.ProductId));
            detailCommand.Parameters.Add(new OracleParameter("p_quantity", detail.Quantity));
            detailCommand.Parameters.Add(new OracleParameter("p_unit_price", detail.UnitPrice));

            await detailCommand.ExecuteNonQueryAsync();
        }

        return saleId;
    }

    public async Task<bool> CancelSaleAsync(int saleId, string? reason)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_cancel_sale";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_sale_id", saleId));
        command.Parameters.Add(new OracleParameter("p_reason", reason ?? (object)DBNull.Value));

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
        // Llamar a la función del paquete Oracle: EM_VITARAIZ_AD.fn_get_sale_balance
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EM_VITARAIZ_AD.fn_get_sale_balance(:p_sale_id) FROM DUAL";
        command.CommandType = System.Data.CommandType.Text;

        command.Parameters.Add(new OracleParameter("p_sale_id", saleId));

        var result = await command.ExecuteScalarAsync();
        decimal balance = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;

        return balance;
    }

    public async Task<string> GetSaleRiskStatusAsync(int saleId)
    {
        // Llamar a la función del paquete Oracle: EM_VITARAIZ_AD.fn_get_risk_status
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EM_VITARAIZ_AD.fn_get_risk_status(:p_sale_id) FROM DUAL";
        command.CommandType = System.Data.CommandType.Text;

        command.Parameters.Add(new OracleParameter("p_sale_id", saleId));

        var result = await command.ExecuteScalarAsync();
        string riskStatus = result != null ? result.ToString() ?? "DESCONOCIDO" : "DESCONOCIDO";

        return riskStatus;
    }
}

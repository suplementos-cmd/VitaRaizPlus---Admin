using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
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

    public async Task<int> CreateSaleAsync(Sale sale)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();
        return sale.SaleId;
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

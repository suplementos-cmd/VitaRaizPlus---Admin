using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly VitaRaizDbContext _context;

    public ProductRepository(VitaRaizDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateProductAsync(string productName, string? description, decimal unitPrice, int stock)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_register_product";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        var productIdParam = new OracleParameter("p_product_id", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(productIdParam);

        command.Parameters.Add(new OracleParameter("p_name", productName));
        command.Parameters.Add(new OracleParameter("p_description", description ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_unit_price", unitPrice));
        command.Parameters.Add(new OracleParameter("p_stock", stock));

        await command.ExecuteNonQueryAsync();

        int productId = Convert.ToInt32(((OracleDecimal)productIdParam.Value).ToInt32());
        return productId;
    }

    public async Task<bool> UpdateProductAsync(int productId, string productName, string? description, 
        decimal unitPrice, int stock, bool isActive)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_update_product";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_product_id", productId));
        command.Parameters.Add(new OracleParameter("p_name", productName));
        command.Parameters.Add(new OracleParameter("p_description", description ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_unit_price", unitPrice));
        command.Parameters.Add(new OracleParameter("p_stock", stock));
        command.Parameters.Add(new OracleParameter("p_is_active", isActive ? 1 : 0));

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_delete_product";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_product_id", productId));

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? searchTerm, bool? isActive)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => 
                p.ProductName.Contains(searchTerm) || 
                (p.Description != null && p.Description.Contains(searchTerm)));
        }

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var products = await query
            .OrderBy(p => p.ProductName)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                Stock = p.Stock,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return products;
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId)
    {
        return await _context.Products
            .Where(p => p.ProductId == productId)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                Stock = p.Stock,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();
    }
}

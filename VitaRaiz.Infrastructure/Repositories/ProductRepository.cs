using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class ProductRepository : BaseOracleRepository, IProductRepository
{
    public ProductRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<int> CreateProductAsync(string productName, string? description, decimal unitPrice, int stock)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_product");

        var productIdParam = AddOutputParameter(command, "p_product_id");

        AddInputParameter(command, "p_name", productName);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_unit_price", unitPrice);
        AddInputParameter(command, "p_stock", stock);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)productIdParam);
    }

    public async Task<bool> UpdateProductAsync(int productId, string productName, string? description, 
        decimal unitPrice, int stock, bool isActive)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_product");

        AddInputParameter(command, "p_product_id", productId);
        AddInputParameter(command, "p_name", productName);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_unit_price", unitPrice);
        AddInputParameter(command, "p_stock", stock);
        AddInputParameter(command, "p_is_active", isActive ? 1 : 0);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_delete_product");

        AddInputParameter(command, "p_product_id", productId);

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

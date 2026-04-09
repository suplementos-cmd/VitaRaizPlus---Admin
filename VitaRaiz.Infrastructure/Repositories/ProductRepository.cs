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

    public async Task<int> CreateProductAsync(string productName, string? description, decimal unitPrice, int stock, int? categoryId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_product");

        var productIdParam = AddOutputParameter(command, "p_product_id");

        AddInputParameter(command, "p_name", productName);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_unit_price", unitPrice);
        AddInputParameter(command, "p_stock", stock);
        AddInputParameter(command, "p_category_id", categoryId);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)productIdParam);
    }

    public async Task<bool> UpdateProductAsync(int productId, string productName, string? description, 
        decimal unitPrice, int stock, bool isActive, int? categoryId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_product");

        AddInputParameter(command, "p_product_id", productId);
        AddInputParameter(command, "p_name", productName);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_unit_price", unitPrice);
        AddInputParameter(command, "p_stock", stock);
        AddInputParameter(command, "p_is_active", isActive ? 1 : 0);
        AddInputParameter(command, "p_category_id", categoryId);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> UpdateProductPhotoAsync(int productId, string photoUrl)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_product_photo");

        AddInputParameter(command, "p_product_id", productId);
        AddInputParameter(command, "p_photo_url", photoUrl);

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
        try
        {
            Console.WriteLine($"[ProductRepository] GetProductsAsync - Params: searchTerm={searchTerm}, isActive={isActive}");
            
            var products = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    UnitPrice = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    Category = p.Category,
                    PhotoUrl = p.PhotoUrl,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            // Filter in-memory to avoid Oracle bool/NUMBER conversion issues
            if (isActive.HasValue)
                products = products.Where(p => p.IsActive == isActive.Value).ToList();

            if (!string.IsNullOrEmpty(searchTerm))
                products = products.Where(p => p.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

            Console.WriteLine($"[ProductRepository] Devolviendo {products.Count} productos");
            return products;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProductRepository] ERROR: {ex.Message}");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId)
    {
        try
        {
            Console.WriteLine($"[ProductRepository] GetProductByIdAsync - productId={productId}");
            
            var product = await _context.Products
                .Where(p => p.ProductId == productId)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    UnitPrice = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    Category = p.Category,
                    PhotoUrl = p.PhotoUrl,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

            Console.WriteLine($"[ProductRepository] Producto encontrado: {product != null}");
            return product;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProductRepository] ERROR en GetProductByIdAsync: {ex.Message}");
            throw;
        }
    }
}

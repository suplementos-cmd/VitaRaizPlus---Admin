using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Interfaces;

public interface ICustomerRepository
{
    Task<int> CreateCustomerAsync(string customerName, string? phoneNumber, string? email, 
        string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted);
    
    Task<bool> UpdateCustomerAsync(int customerId, string customerName, string? phoneNumber, 
        string? email, string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted);
    
    Task<bool> DeleteCustomerAsync(int customerId);
    
    Task<List<CustomerDto>> GetCustomersAsync(string? searchTerm, int? zoneId, 
        bool? isGoldCustomer, bool? isBlacklisted);
    
    Task<CustomerDto?> GetCustomerByIdAsync(int customerId);
}

public interface IProductRepository
{
    Task<int> CreateProductAsync(string productName, string? description, decimal unitPrice, int stock);
    
    Task<bool> UpdateProductAsync(int productId, string productName, string? description, 
        decimal unitPrice, int stock, bool isActive);
    
    Task<bool> DeleteProductAsync(int productId);
    
    Task<List<ProductDto>> GetProductsAsync(string? searchTerm, bool? isActive);
    
    Task<ProductDto?> GetProductByIdAsync(int productId);
}

public interface IZoneRepository
{
    Task<int> CreateZoneAsync(string zoneName, string? description);
    
    Task<bool> UpdateZoneAsync(int zoneId, string zoneName, string? description);
    
    Task<bool> DeleteZoneAsync(int zoneId);
    
    Task<List<ZoneDto>> GetZonesAsync();
    
    Task<ZoneDto?> GetZoneByIdAsync(int zoneId);
}

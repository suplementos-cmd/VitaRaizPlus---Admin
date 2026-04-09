using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Interfaces;

public interface ICustomerRepository
{
    Task<int> CreateCustomerAsync(string customerName, string? phoneNumber, string? email, 
        string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted, int? createdBy = null);
    
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
    Task<int> CreateProductAsync(string productName, string? description, decimal unitPrice, int stock, int? categoryId);
    
    Task<bool> UpdateProductAsync(int productId, string productName, string? description, 
        decimal unitPrice, int stock, bool isActive, int? categoryId);

    Task<bool> UpdateProductPhotoAsync(int productId, string photoUrl);
    
    Task<bool> DeleteProductAsync(int productId);
    
    Task<List<ProductDto>> GetProductsAsync(string? searchTerm, bool? isActive);
    
    Task<ProductDto?> GetProductByIdAsync(int productId);
}

public interface IZoneRepository
{
    Task<int> CreateZoneAsync(string zoneName, string? zoneCode, string? description, bool isActive);
    
    Task<bool> UpdateZoneAsync(int zoneId, string zoneName, string? zoneCode, string? description, bool isActive);
    
    Task<bool> DeleteZoneAsync(int zoneId);
    
    Task<List<ZoneDto>> GetZonesAsync();
    
    Task<ZoneDto?> GetZoneByIdAsync(int zoneId);
}

public interface IUserRepository
{
    Task<List<UserDto>> GetUsersAsync(string? searchTerm, int? roleId, bool? isActive);

    Task<UserDto?> GetUserByIdAsync(int userId);

    Task<int> CreateUserAsync(string username, string fullName, string? email, string password,
        int roleId, int? zoneId, bool isActive, int? createdBy);

    Task<bool> UpdateUserAsync(int userId, string fullName, string? email, int roleId,
        int? zoneId, bool isActive, int? updatedBy);

    Task<bool> DeleteUserAsync(int userId, int? deletedBy);

    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}

public interface IRoleRepository
{
    Task<List<RoleDto>> GetRolesAsync();

    Task<List<string>> GetPermissionsAsync();

    Task<bool> UpdateRoleAsync(int roleId, string roleName, string? description,
        string? defaultThemeColor, int? updatedBy);

    Task<int> CreateRoleAsync(string roleName, string? description, string? defaultThemeColor);

    Task<List<string>> GetRolePermissionsAsync(int roleId);

    Task<bool> SetRolePermissionsAsync(int roleId, List<string> permissions, int? updatedBy);
}

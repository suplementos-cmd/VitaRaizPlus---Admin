using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class CustomerRepository : BaseOracleRepository, ICustomerRepository
{
    public CustomerRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<int> CreateCustomerAsync(string customerName, string? phoneNumber, string? email, 
        string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted, int? createdBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_customer");

        var customerIdParam = AddOutputParameter(command, "p_customer_id");

        AddInputParameter(command, "p_name", customerName);
        AddInputParameter(command, "p_phone", phoneNumber);
        AddInputParameter(command, "p_email", email);
        AddInputParameter(command, "p_address", address);
        AddInputParameter(command, "p_zone_id", zoneId);
        AddInputParameter(command, "p_gps_lat", gpsLatitude);
        AddInputParameter(command, "p_gps_lon", gpsLongitude);
        AddInputParameter(command, "p_is_gold", isGoldCustomer ? 1 : 0);
        AddInputParameter(command, "p_is_blacklisted", isBlacklisted ? 1 : 0);
        AddInputParameter(command, "p_created_by", createdBy.HasValue ? (object)createdBy.Value : DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)customerIdParam);
    }

    public async Task<bool> UpdateCustomerAsync(int customerId, string customerName, string? phoneNumber, 
        string? email, string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_customer");

        AddInputParameter(command, "p_customer_id", customerId);
        AddInputParameter(command, "p_name", customerName);
        AddInputParameter(command, "p_phone", phoneNumber);
        AddInputParameter(command, "p_email", email);
        AddInputParameter(command, "p_address", address);
        AddInputParameter(command, "p_zone_id", zoneId);
        AddInputParameter(command, "p_gps_lat", gpsLatitude);
        AddInputParameter(command, "p_gps_lon", gpsLongitude);
        AddInputParameter(command, "p_is_gold", isGoldCustomer ? 1 : 0);
        AddInputParameter(command, "p_is_blacklisted", isBlacklisted ? 1 : 0);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_delete_customer");

        AddInputParameter(command, "p_customer_id", customerId);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? searchTerm, int? zoneId, 
        bool? isGoldCustomer, bool? isBlacklisted)
    {
        try
        {
            Console.WriteLine($"[CustomerRepository] GetCustomersAsync - Params: searchTerm={searchTerm}, zoneId={zoneId}");
            
            var query = _context.Customers
                .Include(c => c.Zone)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => 
                    c.CustomerName.Contains(searchTerm) || 
                    (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                    (c.Email != null && c.Email.Contains(searchTerm)));
            }

            if (zoneId.HasValue)
                query = query.Where(c => c.ZoneId == zoneId.Value);

            if (isGoldCustomer.HasValue)
                query = query.Where(c => c.IsGoldCustomer == isGoldCustomer.Value);

            if (isBlacklisted.HasValue)
                query = query.Where(c => c.IsBlacklisted == isBlacklisted.Value);

            var customers = await query
                .OrderBy(c => c.CustomerName)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    PhoneNumber = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    ZoneId = c.ZoneId,
                    ZoneName = c.Zone != null ? c.Zone.ZoneName : null,
                    GpsLatitude = c.GpsLatitude,
                    GpsLongitude = c.GpsLongitude,
                    IsGoldCustomer = c.IsGoldCustomer,
                    IsBlacklisted = c.IsBlacklisted,
                    CreatedAt = c.RegisteredAt
                })
                .ToListAsync();

            Console.WriteLine($"[CustomerRepository] Devolviendo {customers.Count} clientes");
            return customers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerRepository] ERROR: {ex.Message}");
            throw;
        }
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId)
    {
        try
        {
            Console.WriteLine($"[CustomerRepository] GetCustomerByIdAsync - customerId={customerId}");
            
            var customer = await _context.Customers
                .Include(c => c.Zone)
                .Where(c => c.CustomerId == customerId)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    PhoneNumber = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    ZoneId = c.ZoneId,
                    ZoneName = c.Zone != null ? c.Zone.ZoneName : null,
                    GpsLatitude = c.GpsLatitude,
                    GpsLongitude = c.GpsLongitude,
                    IsGoldCustomer = c.IsGoldCustomer,
                    IsBlacklisted = c.IsBlacklisted,
                    CreatedAt = c.RegisteredAt
                })
                .FirstOrDefaultAsync();

            Console.WriteLine($"[CustomerRepository] Cliente encontrado: {customer != null}");
            return customer;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerRepository] ERROR en GetCustomerByIdAsync: {ex.Message}");
            throw;
        }
    }
}

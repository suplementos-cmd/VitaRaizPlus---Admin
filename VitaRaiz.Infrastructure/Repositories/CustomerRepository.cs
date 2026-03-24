using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly VitaRaizDbContext _context;

    public CustomerRepository(VitaRaizDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateCustomerAsync(string customerName, string? phoneNumber, string? email, 
        string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_register_customer";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        var customerIdParam = new OracleParameter("p_customer_id", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(customerIdParam);

        command.Parameters.Add(new OracleParameter("p_name", customerName));
        command.Parameters.Add(new OracleParameter("p_phone", phoneNumber ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_email", email ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_address", address ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_zone_id", zoneId ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lat", gpsLatitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lon", gpsLongitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_is_gold", isGoldCustomer ? 1 : 0));
        command.Parameters.Add(new OracleParameter("p_is_blacklisted", isBlacklisted ? 1 : 0));

        await command.ExecuteNonQueryAsync();

        int customerId = Convert.ToInt32(((OracleDecimal)customerIdParam.Value).ToInt32());
        return customerId;
    }

    public async Task<bool> UpdateCustomerAsync(int customerId, string customerName, string? phoneNumber, 
        string? email, string? address, int? zoneId, string? gpsLatitude, string? gpsLongitude, 
        bool isGoldCustomer, bool isBlacklisted)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_update_customer";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_customer_id", customerId));
        command.Parameters.Add(new OracleParameter("p_name", customerName));
        command.Parameters.Add(new OracleParameter("p_phone", phoneNumber ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_email", email ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_address", address ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_zone_id", zoneId ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lat", gpsLatitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lon", gpsLongitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_is_gold", isGoldCustomer ? 1 : 0));
        command.Parameters.Add(new OracleParameter("p_is_blacklisted", isBlacklisted ? 1 : 0));

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_delete_customer";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_customer_id", customerId));

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? searchTerm, int? zoneId, 
        bool? isGoldCustomer, bool? isBlacklisted)
    {
        var query = _context.Customers
            .Include(c => c.Zone)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c => 
                c.CustomerName.Contains(searchTerm) || 
                (c.PhoneNumber != null && c.PhoneNumber.Contains(searchTerm)) ||
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
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Address = c.Address,
                ZoneId = c.ZoneId,
                ZoneName = c.Zone != null ? c.Zone.ZoneName : null,
                GpsLatitude = c.GpsLatitude,
                GpsLongitude = c.GpsLongitude,
                IsGoldCustomer = c.IsGoldCustomer,
                IsBlacklisted = c.IsBlacklisted,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return customers;
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId)
    {
        return await _context.Customers
            .Include(c => c.Zone)
            .Where(c => c.CustomerId == customerId)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Address = c.Address,
                ZoneId = c.ZoneId,
                ZoneName = c.Zone != null ? c.Zone.ZoneName : null,
                GpsLatitude = c.GpsLatitude,
                GpsLongitude = c.GpsLongitude,
                IsGoldCustomer = c.IsGoldCustomer,
                IsBlacklisted = c.IsBlacklisted,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();
    }
}

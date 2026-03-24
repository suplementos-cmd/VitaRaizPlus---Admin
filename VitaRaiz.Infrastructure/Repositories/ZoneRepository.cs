using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class ZoneRepository : IZoneRepository
{
    private readonly VitaRaizDbContext _context;

    public ZoneRepository(VitaRaizDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateZoneAsync(string zoneName, string? description)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_register_zone";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        var zoneIdParam = new OracleParameter("p_zone_id", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(zoneIdParam);

        command.Parameters.Add(new OracleParameter("p_name", zoneName));
        command.Parameters.Add(new OracleParameter("p_description", description ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        int zoneId = Convert.ToInt32(((OracleDecimal)zoneIdParam.Value).ToInt32());
        return zoneId;
    }

    public async Task<bool> UpdateZoneAsync(int zoneId, string zoneName, string? description)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_update_zone";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_zone_id", zoneId));
        command.Parameters.Add(new OracleParameter("p_name", zoneName));
        command.Parameters.Add(new OracleParameter("p_description", description ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteZoneAsync(int zoneId)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_delete_zone";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_zone_id", zoneId));

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<ZoneDto>> GetZonesAsync()
    {
        var zones = await _context.Zones
            .OrderBy(z => z.ZoneName)
            .Select(z => new ZoneDto
            {
                ZoneId = z.ZoneId,
                ZoneName = z.ZoneName,
                Description = z.Description
            })
            .ToListAsync();

        return zones;
    }

    public async Task<ZoneDto?> GetZoneByIdAsync(int zoneId)
    {
        return await _context.Zones
            .Where(z => z.ZoneId == zoneId)
            .Select(z => new ZoneDto
            {
                ZoneId = z.ZoneId,
                ZoneName = z.ZoneName,
                Description = z.Description
            })
            .FirstOrDefaultAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class ZoneRepository : BaseOracleRepository, IZoneRepository
{
    public ZoneRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<int> CreateZoneAsync(string zoneName, string? description)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_zone");

        var zoneIdParam = AddOutputParameter(command, "p_zone_id");

        AddInputParameter(command, "p_name", zoneName);
        AddInputParameter(command, "p_description", description);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)zoneIdParam);
    }

    public async Task<bool> UpdateZoneAsync(int zoneId, string zoneName, string? description)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_zone");

        AddInputParameter(command, "p_zone_id", zoneId);
        AddInputParameter(command, "p_name", zoneName);
        AddInputParameter(command, "p_description", description);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteZoneAsync(int zoneId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_delete_zone");

        AddInputParameter(command, "p_zone_id", zoneId);

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

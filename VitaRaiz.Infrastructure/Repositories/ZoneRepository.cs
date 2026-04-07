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

    public async Task<int> CreateZoneAsync(string zoneName, string? zoneCode, string? description, bool isActive)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_zone");

        var zoneIdParam = AddOutputParameter(command, "p_zone_id");

        AddInputParameter(command, "p_name", zoneName);
        AddInputParameter(command, "p_zone_code", zoneCode);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_is_active", isActive ? 1 : 0);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)zoneIdParam);
    }

    public async Task<bool> UpdateZoneAsync(int zoneId, string zoneName, string? zoneCode, string? description, bool isActive)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_zone");

        AddInputParameter(command, "p_zone_id", zoneId);
        AddInputParameter(command, "p_name", zoneName);
        AddInputParameter(command, "p_zone_code", zoneCode);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_is_active", isActive ? 1 : 0);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteZoneAsync(int zoneId)
    {
        try
        {
            var connection = await GetOpenConnectionAsync();
            using var command = CreatePackageProcedureCommand(connection, "sp_delete_zone");
            AddInputParameter(command, "p_zone_id", zoneId);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2292)
        {
            throw new InvalidOperationException(
                "La zona tiene clientes u otros registros asignados y no puede eliminarse. " +
                "Reasigne los clientes a otra zona o marque la zona como inactiva.", ex);
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2291)
        {
            throw new InvalidOperationException(
                "No se puede eliminar: existe una referencia de integridad relacionada.", ex);
        }
    }

    public async Task<List<ZoneDto>> GetZonesAsync()
    {
        try
        {
            Console.WriteLine("[ZoneRepository] GetZonesAsync");
            
            var zones = await _context.Zones
                .OrderBy(z => z.ZoneName)
                .Select(z => new ZoneDto
                {
                    ZoneId = z.ZoneId,
                    ZoneName = z.ZoneName,
                    ZoneCode = z.ZoneCode,
                    Description = z.Description,
                    IsActive = z.IsActive,
                    CreatedAt = z.CreatedAt
                })
                .ToListAsync();

            Console.WriteLine($"[ZoneRepository] Devolviendo {zones.Count} zonas");
            return zones;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ZoneRepository] ERROR: {ex.Message}");
            throw;
        }
    }

    public async Task<ZoneDto?> GetZoneByIdAsync(int zoneId)
    {
        try
        {
            Console.WriteLine($"[ZoneRepository] GetZoneByIdAsync - zoneId={zoneId}");
            
            var zone = await _context.Zones
                .Where(z => z.ZoneId == zoneId)
                .Select(z => new ZoneDto
                {
                    ZoneId = z.ZoneId,
                    ZoneName = z.ZoneName,
                    ZoneCode = z.ZoneCode,
                    Description = z.Description,
                    IsActive = z.IsActive,
                    CreatedAt = z.CreatedAt
                })
                .FirstOrDefaultAsync();

            Console.WriteLine($"[ZoneRepository] Zona encontrada: {zone != null}");
            return zone;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ZoneRepository] ERROR en GetZoneByIdAsync: {ex.Message}");
            throw;
        }
    }
}

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class RoleRepository : BaseOracleRepository, IRoleRepository
{
    public RoleRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_roles");

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var roles = new List<RoleDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            roles.Add(new RoleDto
            {
                RoleId = reader.GetInt32("roleId"),
                RoleName = reader.GetString("roleName"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                DefaultThemeColor = reader.IsDBNull("defaultThemeColor") ? null : reader.GetString("defaultThemeColor")
            });
        }

        return roles;
    }

    public async Task<List<string>> GetPermissionsAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_permissions");

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var permissions = new List<string>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull("permissionName"))
                permissions.Add(reader.GetString("permissionName"));
        }

        return permissions;
    }

    public async Task<bool> UpdateRoleAsync(int roleId, string roleName, string? description,
        string? defaultThemeColor, int? updatedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_role");

        AddInputParameter(command, "p_role_id", roleId);
        AddInputParameter(command, "p_role_name", roleName);
        AddInputParameter(command, "p_description", description);
        AddInputParameter(command, "p_default_theme_color", defaultThemeColor);
        AddInputParameter(command, "p_updated_by", updatedBy);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<string>> GetRolePermissionsAsync(int roleId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_role_permissions");

        AddInputParameter(command, "p_role_id", roleId);

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var permissions = new List<string>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull("permissionName"))
                permissions.Add(reader.GetString("permissionName"));
        }

        return permissions;
    }

    public async Task<bool> SetRolePermissionsAsync(int roleId, List<string> permissions, int? updatedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_set_role_permissions");

        var cleanPermissions = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        AddInputParameter(command, "p_role_id", roleId);
        AddInputParameter(command, "p_permissions_csv", string.Join(",", cleanPermissions), OracleDbType.Clob);
        AddInputParameter(command, "p_updated_by", updatedBy);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<int> CreateRoleAsync(string roleName, string? description, string? defaultThemeColor)
    {
        var connection = await GetOpenConnectionAsync();

        // Get next sequence value
        using var seqCmd = connection.CreateCommand();
        seqCmd.CommandType = CommandType.Text;
        seqCmd.CommandText = "SELECT SEQ_ROLES.NEXTVAL FROM DUAL";
        var newId = Convert.ToInt32(await seqCmd.ExecuteScalarAsync());

        using var insCmd = connection.CreateCommand();
        insCmd.CommandType = CommandType.Text;
        insCmd.CommandText = @"
            INSERT INTO SALESAPP.ROLES (ROLE_ID, ROLE_NAME, ROLE_DESCRIPTION, DEFAULT_THEME_COLOR)
            VALUES (:p_id, :p_name, :p_desc, :p_color)";

        AddInputParameter(insCmd, "p_id",    newId);
        AddInputParameter(insCmd, "p_name",  roleName);
        AddInputParameter(insCmd, "p_desc",  description);
        AddInputParameter(insCmd, "p_color", defaultThemeColor);
        await insCmd.ExecuteNonQueryAsync();

        return newId;
    }
}

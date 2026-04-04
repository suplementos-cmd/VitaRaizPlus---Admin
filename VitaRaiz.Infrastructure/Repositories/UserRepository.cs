using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class UserRepository : BaseOracleRepository, IUserRepository
{
    public UserRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<List<UserDto>> GetUsersAsync(string? searchTerm, int? roleId, bool? isActive)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_users");

        AddInputParameter(command, "p_search_term", searchTerm);
        AddInputParameter(command, "p_role_id", roleId);
        AddInputParameter(command, "p_is_active", isActive.HasValue ? (isActive.Value ? 1 : 0) : null);

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var users = new List<UserDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            users.Add(new UserDto
            {
                UserId = reader.GetInt32("userId"),
                Username = reader.GetString("username"),
                FullName = reader.IsDBNull("fullName") ? string.Empty : reader.GetString("fullName"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                RoleId = reader.GetInt32("roleId"),
                RoleName = reader.IsDBNull("roleName") ? null : reader.GetString("roleName"),
                ZoneId = reader.IsDBNull("zoneId") ? null : reader.GetInt32("zoneId"),
                ZoneName = reader.IsDBNull("zoneName") ? null : reader.GetString("zoneName"),
                IsActive = reader.GetInt32("isActive") == 1,
                CreatedAt = reader.IsDBNull("createdAt") ? null : reader.GetDateTime("createdAt"),
                LastLogin = reader.IsDBNull("lastLogin") ? null : reader.GetDateTime("lastLogin")
            });
        }

        return users;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_user_by_id");

        AddInputParameter(command, "p_user_id", userId);

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        if (await reader.ReadAsync())
        {
            return new UserDto
            {
                UserId = reader.GetInt32("userId"),
                Username = reader.GetString("username"),
                FullName = reader.IsDBNull("fullName") ? string.Empty : reader.GetString("fullName"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                RoleId = reader.GetInt32("roleId"),
                RoleName = reader.IsDBNull("roleName") ? null : reader.GetString("roleName"),
                ZoneId = reader.IsDBNull("zoneId") ? null : reader.GetInt32("zoneId"),
                ZoneName = reader.IsDBNull("zoneName") ? null : reader.GetString("zoneName"),
                IsActive = reader.GetInt32("isActive") == 1,
                CreatedAt = reader.IsDBNull("createdAt") ? null : reader.GetDateTime("createdAt"),
                LastLogin = reader.IsDBNull("lastLogin") ? null : reader.GetDateTime("lastLogin")
            };
        }

        return null;
    }

    public async Task<int> CreateUserAsync(string username, string fullName, string? email, string password,
        int roleId, int? zoneId, bool isActive, int? createdBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_user");

        var userIdParam = AddOutputParameter(command, "p_user_id");

        AddInputParameter(command, "p_username", username);
        AddInputParameter(command, "p_full_name", fullName);
        AddInputParameter(command, "p_email", email);
        AddInputParameter(command, "p_password", password);
        AddInputParameter(command, "p_role_id", roleId);
        AddInputParameter(command, "p_zone_id", zoneId);
        AddInputParameter(command, "p_is_active", isActive ? 1 : 0);
        AddInputParameter(command, "p_created_by", createdBy);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue(userIdParam);
    }

    public async Task<bool> UpdateUserAsync(int userId, string fullName, string? email, int roleId,
        int? zoneId, bool isActive, int? updatedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_user");

        AddInputParameter(command, "p_user_id", userId);
        AddInputParameter(command, "p_full_name", fullName);
        AddInputParameter(command, "p_email", email);
        AddInputParameter(command, "p_role_id", roleId);
        AddInputParameter(command, "p_zone_id", zoneId);
        AddInputParameter(command, "p_is_active", isActive ? 1 : 0);
        AddInputParameter(command, "p_updated_by", updatedBy);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId, int? deletedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_delete_user");

        AddInputParameter(command, "p_user_id", userId);
        AddInputParameter(command, "p_deleted_by", deletedBy);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var connection = await GetOpenConnectionAsync();
        var parameters = new Dictionary<string, object>
        {
            { "p_user_id", userId },
            { "p_current_password", currentPassword },
            { "p_new_password", newPassword }
        };

        using var command = CreatePackageStringFunctionCommand(connection, "fn_change_user_password", parameters);
        await command.ExecuteNonQueryAsync();

        var resultParam = (OracleParameter)command.Parameters["result"];
        var result = resultParam.Value?.ToString();
        return result == "1";
    }
}

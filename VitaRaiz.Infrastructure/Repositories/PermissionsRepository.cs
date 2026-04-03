using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class PermissionsRepository : BaseOracleRepository, IPermissionsRepository
{
    public PermissionsRepository(VitaRaizDbContext context) : base(context) { }

    public async Task<List<string>> GetUserPermissionsAsync(int userId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_user_permissions");

        command.Parameters.Add(new OracleParameter("p_user_id", OracleDbType.Int32) { Value = userId });
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
}

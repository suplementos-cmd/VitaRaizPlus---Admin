using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using VitaRaiz.Infrastructure.Configuration;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

/// <summary>
/// Clase base para repositorios que interactúan con el paquete Oracle
/// </summary>
public abstract class BaseOracleRepository
{
    protected readonly VitaRaizDbContext _context;

    protected BaseOracleRepository(VitaRaizDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene y abre la conexión a la base de datos
    /// </summary>
    protected async Task<DbConnection> GetOpenConnectionAsync()
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Crea un comando para ejecutar un stored procedure del paquete Oracle
    /// </summary>
    protected DbCommand CreatePackageProcedureCommand(DbConnection connection, string procedureName)
    {
        var command = connection.CreateCommand();
        command.CommandText = OraclePackageConfig.GetProcedureName(procedureName);
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    /// <summary>
    /// Crea un comando para ejecutar una función del paquete Oracle usando bloque PL/SQL
    /// </summary>
    protected DbCommand CreatePackageFunctionCommand(DbConnection connection, string functionName, 
        Dictionary<string, object> parameters)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;

        // Crear parámetro de salida
        var resultParam = new OracleParameter("result", OracleDbType.Decimal)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(resultParam);

        // Crear parámetros de entrada
        var paramNames = new List<string>();
        foreach (var param in parameters)
        {
            var oracleParam = new OracleParameter(param.Key, param.Value ?? DBNull.Value);
            command.Parameters.Add(oracleParam);
            paramNames.Add($":{param.Key}");
        }

        // Construir llamada a función
        var paramList = string.Join(", ", paramNames);
        command.CommandText = OraclePackageConfig.GetFunctionCall(functionName, paramList);

        return command;
    }

    /// <summary>
    /// Crea un comando para ejecutar una función del paquete Oracle que retorna VARCHAR2
    /// </summary>
    protected DbCommand CreatePackageStringFunctionCommand(DbConnection connection, string functionName, 
        Dictionary<string, object> parameters)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;

        // Crear parámetro de salida para VARCHAR2
        var resultParam = new OracleParameter("result", OracleDbType.Varchar2, 4000)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(resultParam);

        // Crear parámetros de entrada
        var paramNames = new List<string>();
        foreach (var param in parameters)
        {
            var oracleParam = new OracleParameter(param.Key, param.Value ?? DBNull.Value);
            command.Parameters.Add(oracleParam);
            paramNames.Add($":{param.Key}");
        }

        // Construir llamada a función
        var paramList = string.Join(", ", paramNames);
        command.CommandText = OraclePackageConfig.GetFunctionCall(functionName, paramList);

        return command;
    }

    /// <summary>
    /// Agrega un parámetro OUT de tipo entero al comando
    /// </summary>
    protected OracleParameter AddOutputParameter(DbCommand command, string parameterName)
    {
        var param = new OracleParameter(parameterName, OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(param);
        return param;
    }

    /// <summary>
    /// Agrega un parámetro de entrada al comando
    /// </summary>
    protected void AddInputParameter(DbCommand command, string parameterName, object value, 
        OracleDbType? dbType = null)
    {
        var param = dbType.HasValue 
            ? new OracleParameter(parameterName, dbType.Value) 
            : new OracleParameter(parameterName, value ?? DBNull.Value);
        
        if (dbType.HasValue)
            param.Value = value ?? DBNull.Value;
            
        command.Parameters.Add(param);
    }

    /// <summary>
    /// Extrae el valor de un parámetro OUT
    /// </summary>
    protected int GetOutputValue(OracleParameter parameter)
    {
        return Convert.ToInt32(((Oracle.ManagedDataAccess.Types.OracleDecimal)parameter.Value).ToInt32());
    }
}

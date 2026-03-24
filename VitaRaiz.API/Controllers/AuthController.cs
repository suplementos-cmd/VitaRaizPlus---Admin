using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VitaRaiz.Infrastructure.Data;
using VitaRaiz.API.Services;
using VitaRaiz.Infrastructure.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace VitaRaiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly VitaRaizDbContext _context;
    private readonly JwtTokenService _jwtService;

    public AuthController(VitaRaizDbContext context, JwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        var connection = _context.Database.GetDbConnection();
        
        try
        {
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // Llamar a fn_authenticate_user usando bloque PL/SQL
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = OraclePackageConfig.GetFunctionCall("fn_authenticate_user", ":p_username, :p_password");

            var resultParam = new OracleParameter("result", OracleDbType.Decimal)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(resultParam);
            command.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2) { Value = request.Username });
            command.Parameters.Add(new OracleParameter("p_password", OracleDbType.Varchar2) { Value = request.Password });

            await command.ExecuteNonQueryAsync();

            var result = resultParam.Value;

            // Si la función devuelve NULL, las credenciales son inválidas
            if (result == null || result == DBNull.Value)
            {
                return Unauthorized(new { error = "Usuario o contraseña inválidos" });
            }

            int userId = Convert.ToInt32(((OracleDecimal)result).Value);

            // Actualizar último login usando el stored procedure
            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = OraclePackageConfig.GetProcedureName("sp_update_last_login");
            updateCmd.CommandType = CommandType.StoredProcedure;
            updateCmd.Parameters.Add(new OracleParameter("p_user_id", OracleDbType.Int32) { Value = userId });
            await updateCmd.ExecuteNonQueryAsync();

            // Obtener información del usuario usando stored procedure
            using var userCmd = connection.CreateCommand();
            userCmd.CommandText = OraclePackageConfig.GetProcedureName("sp_get_user_info");
            userCmd.CommandType = CommandType.StoredProcedure;

            userCmd.Parameters.Add(new OracleParameter("p_user_id", OracleDbType.Int32) { Value = userId });
            
            var usernameParam = new OracleParameter("p_username", OracleDbType.Varchar2, 100)
            {
                Direction = ParameterDirection.Output
            };
            var roleNameParam = new OracleParameter("p_role_name", OracleDbType.Varchar2, 50)
            {
                Direction = ParameterDirection.Output
            };
            var roleIdParam = new OracleParameter("p_role_id", OracleDbType.Int32)
            {
                Direction = ParameterDirection.Output
            };
            
            userCmd.Parameters.Add(usernameParam);
            userCmd.Parameters.Add(roleNameParam);
            userCmd.Parameters.Add(roleIdParam);

            await userCmd.ExecuteNonQueryAsync();
            
            if (usernameParam.Value == null || usernameParam.Value == DBNull.Value)
            {
                return Unauthorized(new { error = "Usuario no encontrado" });
            }

            string username = usernameParam.Value.ToString() ?? request.Username;
            string roleName = roleNameParam.Value?.ToString() ?? "Usuario";

            // Generar token JWT
            var token = _jwtService.GenerateToken(userId, username, roleName);

            return Ok(new
            {
                token,
                userId,
                username,
                role = roleName,
                expiresIn = 480 * 60 // en segundos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al autenticar usuario", details = ex.Message });
        }
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

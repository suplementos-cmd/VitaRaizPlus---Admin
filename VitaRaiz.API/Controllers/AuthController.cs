using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VitaRaiz.Infrastructure.Data;
using VitaRaiz.API.Services;
using VitaRaiz.Infrastructure.Configuration;
using VitaRaiz.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace VitaRaiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly VitaRaizDbContext _context;
    private readonly JwtTokenService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly IPermissionsRepository _permissionsRepository;

    public AuthController(VitaRaizDbContext context, JwtTokenService jwtService,
        ILogger<AuthController> logger, IPermissionsRepository permissionsRepository)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _permissionsRepository = permissionsRepository;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("[AuthController] Login attempt for user: {Username}", request.Username);
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
                _logger.LogWarning("[AuthController] Failed login attempt for user: {Username}", request.Username);
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
            int roleId = 0;
            if (roleIdParam.Value != null && roleIdParam.Value != DBNull.Value)
                roleId = Convert.ToInt32(((Oracle.ManagedDataAccess.Types.OracleDecimal)roleIdParam.Value).Value);

            // Generar token JWT con roleId embebido para tema contextual
            var token = _jwtService.GenerateToken(userId, username, roleName, roleId);

            return Ok(new
            {
                token,
                userId,
                username,
                role = roleName,
                roleId,
                expiresIn = 480 * 60 // en segundos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al autenticar usuario", details = ex.Message });
        }
    }

    [HttpGet("validate")]
    [Authorize]
    public ActionResult ValidateToken()
    {
        try
        {
            // Si llegamos aquí, el token es válido (ya lo validó el middleware JWT)
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                        ?? User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value;

            return Ok(new
            {
                valid = true,
                userId = userId,
                username = username,
                role = role
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al validar token", details = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                        ?? User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Token inválido o expirado" });
            }

            return Ok(new
            {
                userId = int.Parse(userId),
                username = username,
                role = role
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener información del usuario", details = ex.Message });
        }
    }

    /// <summary>
    /// Retorna la lista de permisos (permission codes) asignados al rol del usuario autenticado.
    /// GET /api/auth/permissions
    /// </summary>
    [HttpGet("permissions")]
    [Authorize]
    public async Task<ActionResult> GetMyPermissions()
    {
        try
        {
            var userIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { error = "Token inválido o expirado" });

            _logger.LogInformation("[AuthController] GetMyPermissions for userId={UserId}", userId);

            var permissions = await _permissionsRepository.GetUserPermissionsAsync(userId);

            return Ok(new { permissions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthController] Error getting permissions");
            return StatusCode(500, new { error = "Error al obtener permisos", details = ex.Message });
        }
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VitaRaiz.Infrastructure.Data;
using VitaRaiz.API.Services;

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
        // Validar usuario
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null)
        {
            return Unauthorized(new { error = "Usuario o contraseña inválidos" });
        }

        // TODO: Implementar verificación de hash de contraseña usando BCrypt u otro algoritmo
        // Por ahora, comparación simple (NO USAR EN PRODUCCIÓN)
        if (user.PasswordHash != request.Password)
        {
            return Unauthorized(new { error = "Usuario o contraseña inválidos" });
        }

        // Actualizar último login
        user.LastLogin = DateTime.Now;
        await _context.SaveChangesAsync();

        // Generar token JWT
        var token = _jwtService.GenerateToken(user.UserId, user.Username, user.Role?.RoleName ?? "Usuario");

        return Ok(new
        {
            token,
            userId = user.UserId,
            username = user.Username,
            role = user.Role?.RoleName,
            expiresIn = 480 * 60 // en segundos
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

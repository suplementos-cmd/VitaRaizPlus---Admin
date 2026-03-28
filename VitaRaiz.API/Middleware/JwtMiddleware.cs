using System.Text.RegularExpressions;

namespace VitaRaiz.API.Middleware;

/// <summary>
/// Middleware que normaliza el header Authorization para permitir tokens con o sin prefijo "Bearer"
/// Esto hace la API más flexible para clientes móviles
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader))
        {
            // Si el token no comienza con "Bearer ", añadirlo
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Verificar que parece un JWT (tiene formato xxx.yyy.zzz)
                if (Regex.IsMatch(authHeader.Trim(), @"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$"))
                {
                    _logger.LogDebug("Normalizando token JWT sin prefijo Bearer");
                    context.Request.Headers["Authorization"] = $"Bearer {authHeader.Trim()}";
                }
            }
        }

        await _next(context);
    }
}

using VitaRaiz.WebPortal.Models;
using NLog;

namespace VitaRaiz.WebPortal.Services;

public class AuthService
{
    private readonly ApiService _api;
    private readonly CustomAuthenticationStateProvider _authProvider;
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    public AuthService(ApiService api, CustomAuthenticationStateProvider authProvider)
    {
        _api          = api;
        _authProvider = authProvider;
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _api.PostUnauthAsync<LoginRequest, AuthResponse>(
                "api/auth/login",
                new LoginRequest(username, password));

            if (response is null)
                return (false, "No se pudo conectar con el servidor");

            if (string.IsNullOrWhiteSpace(response.Token))
            {
                var msg = !string.IsNullOrEmpty(response.Error)   ? response.Error
                        : !string.IsNullOrEmpty(response.Message) ? response.Message
                        : "Credenciales incorrectas";
                return (false, msg);
            }

            var user = new CurrentUser
            {
                UserId   = response.UserId,
                Username = response.Username,
                Role     = response.Role,
                RoleId   = response.RoleId,
                Token    = response.Token,
            };

            await _authProvider.MarkAuthenticatedAsync(user);
            _log.Info("User {Username} ({Role}) logged in", user.Username, user.Role);
            return (true, "OK");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "LoginAsync error");
            return (false, "Error inesperado al iniciar sesión");
        }
    }

    public async Task LogoutAsync()
    {
        await _authProvider.MarkLoggedOutAsync();
        _log.Info("User logged out");
    }

    public async Task<CurrentUser?> GetCurrentUserAsync()
        => await _authProvider.GetCurrentUserAsync();
}

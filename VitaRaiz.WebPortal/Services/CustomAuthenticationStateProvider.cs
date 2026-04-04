using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using VitaRaiz.WebPortal.Models;
using NLog;

namespace VitaRaiz.WebPortal.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _session;
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    // ── In-memory cache ──────────────────────────────────────────────────────
    // Scoped per Blazor circuit (browser tab). Survives page-to-page navigation
    // within the same circuit without requiring JS interop on every auth check.
    // On page refresh a new circuit is created, the cache starts empty and is
    // repopulated on the first successful ProtectedSessionStorage read.
    private AuthenticationState? _cachedState;
    private CurrentUser?         _cachedUser;
    // ─────────────────────────────────────────────────────────────────────────

    public CustomAuthenticationStateProvider(ProtectedSessionStorage session)
        => _session = session;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Fast path: already authenticated this circuit — no JS interop needed.
        if (_cachedState is not null)
            return _cachedState;

        try
        {
            var result = await _session.GetAsync<CurrentUser>("currentUser");
            var user   = result.Success ? result.Value : null;
            if (user is null || string.IsNullOrWhiteSpace(user.Token))
                return _anonymous;

            _cachedUser  = user;
            _cachedState = BuildState(user);
            return _cachedState;
        }
        catch (Exception ex)
        {
            // Storage not ready yet (circuit initialising) — return anonymous
            // silently; the cascade will re-evaluate once the circuit is live.
            _log.Warn(ex, "GetAuthenticationStateAsync — storage not ready, returning anonymous");
            return _anonymous;
        }
    }

    public async Task<CurrentUser?> GetCurrentUserAsync()
    {
        // Fast path: return in-memory cached user (no JS interop).
        if (_cachedUser is not null)
            return _cachedUser;

        try
        {
            var result = await _session.GetAsync<CurrentUser>("currentUser");
            if (result.Success && result.Value is not null
                               && !string.IsNullOrWhiteSpace(result.Value.Token))
            {
                _cachedUser  = result.Value;
                _cachedState = BuildState(_cachedUser);
            }
            return _cachedUser;
        }
        catch { return null; }
    }

    public async Task MarkAuthenticatedAsync(CurrentUser user)
    {
        await _session.SetAsync("currentUser", user);
        // Populate cache immediately so subsequent calls in the same request
        // cycle don't need to go back to storage.
        _cachedUser  = user;
        _cachedState = BuildState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    public async Task MarkLoggedOutAsync()
    {
        await _session.DeleteAsync("currentUser");
        _cachedUser  = null;
        _cachedState = null;
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    private AuthenticationState BuildState(CurrentUser user) =>
        new(new ClaimsPrincipal(BuildIdentity(user)));

    private static ClaimsIdentity BuildIdentity(CurrentUser user) =>
        new(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role),
        }, "VitaRaizAuth");
}

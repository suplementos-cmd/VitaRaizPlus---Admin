namespace VitaRaiz.Domain.Constants;

/// <summary>
/// Role name constants as returned by the API (case-insensitive matching intended).
/// Use the helper methods with StringComparison.OrdinalIgnoreCase to match
/// role strings from SecureStorage (e.g. "Vendedor", "VENDEDOR", "vendedor").
/// </summary>
public static class Roles
{
    public const string Admin            = "admin";
    public const string AdminRH          = "admin_rh";
    public const string SupervisorVentas = "supervisor";
    public const string SupervisorCobros = "supervisor_cobros";
    public const string Vendedor         = "vendedor";
    public const string Cobrador         = "cobrador";

    // ── Matching helpers ──────────────────────────────────────────────────

    public static bool IsVendedor(string role)
        => role.Contains(Vendedor, StringComparison.OrdinalIgnoreCase);

    public static bool IsCobrador(string role)
        => role.Contains(Cobrador, StringComparison.OrdinalIgnoreCase);

    /// <summary>Supervisor de cobros (role contains "supervisor" AND "cobro").</summary>
    public static bool IsSupervisorCobros(string role)
        => role.Contains("supervisor", StringComparison.OrdinalIgnoreCase)
        && role.Contains("cobro",      StringComparison.OrdinalIgnoreCase);

    /// <summary>Supervisor de ventas (role contains "supervisor" but NOT "cobro").</summary>
    public static bool IsSupervisorVentas(string role)
        => role.Contains("supervisor", StringComparison.OrdinalIgnoreCase)
        && !role.Contains("cobro",     StringComparison.OrdinalIgnoreCase);

    /// <summary>Admin RH (role is "admin_rh" or contains "admin" AND "rh").</summary>
    public static bool IsAdminRH(string role)
        => role.Equals("admin_rh", StringComparison.OrdinalIgnoreCase)
        || (role.Contains("admin", StringComparison.OrdinalIgnoreCase)
            && role.Contains("rh",   StringComparison.OrdinalIgnoreCase));

    /// <summary>Full admin (role contains "admin" but is not admin_rh).</summary>
    public static bool IsAdminFull(string role)
        => role.Contains("admin", StringComparison.OrdinalIgnoreCase)
        && !role.Contains("rh",   StringComparison.OrdinalIgnoreCase);

    /// <summary>Any supervisor or admin role.</summary>
    public static bool IsManagerOrAbove(string role)
        => role.Contains("supervisor", StringComparison.OrdinalIgnoreCase)
        || role.Contains("admin",      StringComparison.OrdinalIgnoreCase);
}

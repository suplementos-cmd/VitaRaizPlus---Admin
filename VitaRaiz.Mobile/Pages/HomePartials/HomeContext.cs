using System.ComponentModel;
using System.Runtime.CompilerServices;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages.HomePartials;

/// <summary>
/// Shared ViewModel injected into every Home partial view.
/// <para>
/// Populated by <c>HomePage.LoadDataAsync()</c> before the partial is created.
/// Stats are observable: bindings in partial views update automatically when
/// values change (e.g. on pull-to-refresh).
/// </para>
/// <para>
/// Extended stats (TeamSalesToday, TeamPaymentsToday, OverdueCount, PendingApprovals)
/// are zero by default; supervisor / admin partials load them via <c>Api</c>.
/// </para>
/// </summary>
public sealed class HomeContext : INotifyPropertyChanged
{
    // ── Identity ─────────────────────────────────────────────────────────
    public int    UserId   { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Role     { get; init; } = string.Empty;

    // ── Services ─────────────────────────────────────────────────────────
    public ApiService         Api         { get; init; } = null!;
    public PermissionsService Permissions { get; init; } = null!;

    // ── Base stats (always loaded by HomePage) ────────────────────────────
    private decimal _todayPayments;
    public decimal TodayPayments
    {
        get => _todayPayments;
        set { _todayPayments = value; OnPropertyChanged(); }
    }

    private int _todaySales;
    public int TodaySales
    {
        get => _todaySales;
        set { _todaySales = value; OnPropertyChanged(); }
    }

    private decimal _pendingAmount;
    public decimal PendingAmount
    {
        get => _pendingAmount;
        set { _pendingAmount = value; OnPropertyChanged(); }
    }

    private int _todayVisits;
    public int TodayVisits
    {
        get => _todayVisits;
        set { _todayVisits = value; OnPropertyChanged(); }
    }

    // ── Extended stats (loaded by supervisor / admin partials) ────────────
    private int _teamSalesToday;
    public int TeamSalesToday
    {
        get => _teamSalesToday;
        set { _teamSalesToday = value; OnPropertyChanged(); }
    }

    private decimal _teamPaymentsToday;
    public decimal TeamPaymentsToday
    {
        get => _teamPaymentsToday;
        set { _teamPaymentsToday = value; OnPropertyChanged(); }
    }

    private int _overdueCount;
    public int OverdueCount
    {
        get => _overdueCount;
        set { _overdueCount = value; OnPropertyChanged(); }
    }

    private int _pendingApprovals;
    public int PendingApprovals
    {
        get => _pendingApprovals;
        set { _pendingApprovals = value; OnPropertyChanged(); }
    }

    // ── Navigation delegates (wired up in HomePage.LoadDataAsync) ─────────
    public Func<Task> GoToSales         { get; init; } = () => Task.CompletedTask;
    public Func<Task> GoToCustomers     { get; init; } = () => Task.CompletedTask;
    public Func<Task> GoToCreateSale    { get; init; } = () => Task.CompletedTask;
    public Func<Task> GoToRegistroCobro { get; init; } = () => Task.CompletedTask;

    // ── INotifyPropertyChanged ────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

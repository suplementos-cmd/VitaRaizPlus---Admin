using MudBlazor;

namespace VitaRaiz.WebPortal.Services;

/// <summary>Thin wrapper around ISnackbar for consistent notification calls.</summary>
public class NotificationService
{
    private readonly ISnackbar _snack;
    public NotificationService(ISnackbar snack) => _snack = snack;

    public void Success(string msg) => _snack.Add(msg, Severity.Success);
    public void Error(string msg)   => _snack.Add(msg, Severity.Error);
    public void Warning(string msg) => _snack.Add(msg, Severity.Warning);
    public void Info(string msg)    => _snack.Add(msg, Severity.Info);
}

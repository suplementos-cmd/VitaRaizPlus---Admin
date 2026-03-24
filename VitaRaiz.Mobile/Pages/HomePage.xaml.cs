using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace VitaRaiz.Mobile.Pages;

public partial class HomePage : ContentPage, INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private string _username = string.Empty;
    private string _role = string.Empty;
    private decimal _todayPayments;
    private int _todaySales;
    private decimal _pendingAmount;
    private int _todayVisits;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
        
        _apiService = MauiApp.Current?.Handler?.MauiContext?.Services.GetService<ApiService>() ?? new ApiService();
        
        GoToPaymentsCommand = new Command(async () => await Shell.Current.GoToAsync("//PaymentPage"));
        GoToSalesCommand = new Command(async () => await Shell.Current.GoToAsync("//SalesPage"));
        GoToCustomersCommand = new Command(async () => await Shell.Current.GoToAsync("//CustomersPage"));
        
        _ = LoadDataAsync();
    }

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }

    public string Role
    {
        get => _role;
        set
        {
            _role = value;
            OnPropertyChanged();
        }
    }

    public decimal TodayPayments
    {
        get => _todayPayments;
        set
        {
            _todayPayments = value;
            OnPropertyChanged();
        }
    }

    public int TodaySales
    {
        get => _todaySales;
        set
        {
            _todaySales = value;
            OnPropertyChanged();
        }
    }

    public decimal PendingAmount
    {
        get => _pendingAmount;
        set
        {
            _pendingAmount = value;
            OnPropertyChanged();
        }
    }

    public int TodayVisits
    {
        get => _todayVisits;
        set
        {
            _todayVisits = value;
            OnPropertyChanged();
        }
    }

    public ICommand GoToPaymentsCommand { get; }
    public ICommand GoToSalesCommand { get; }
    public ICommand GoToCustomersCommand { get; }

    private async Task LoadDataAsync()
    {
        try
        {
            // Cargar datos del usuario desde SecureStorage
            Username = await SecureStorage.GetAsync("username") ?? "Usuario";
            Role = await SecureStorage.GetAsync("role") ?? "Cobrador";
            var userIdStr = await SecureStorage.GetAsync("user_id");
            int userId = int.TryParse(userIdStr, out var id) ? id : 0;
            
            // Cargar estadísticas reales desde la API
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            
            // Obtener pagos de hoy del usuario
            var paymentsParams = new Dictionary<string, string?>
            {
                { "collectorId", userId.ToString() },
                { "startDate", today },
                { "endDate", today }
            };
            var paymentsToday = await _apiService.GetAsync<List<PaymentDto>>("/api/payments", paymentsParams);
            TodayPayments = paymentsToday?.Where(p => p.Status == "approved").Sum(p => p.Amount) ?? 0;
            TodayVisits = paymentsToday?.Count ?? 0;
            
            // Obtener ventas de hoy del usuario (si es vendedor)
            if (Role.Contains("Vendedor", StringComparison.OrdinalIgnoreCase) || 
                Role.Contains("Supervisor", StringComparison.OrdinalIgnoreCase) ||
                Role.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var salesParams = new Dictionary<string, string?>
                {
                    { "sellerId", userId.ToString() },
                    { "startDate", today },
                    { "endDate", today }
                };
                var salesToday = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("/api/sales", salesParams);
                TodaySales = salesToday?.Count ?? 0;
            }
            
            // Obtener ventas activas asignadas al cobrador
            var activeSalesParams = new Dictionary<string, string?>
            {
                { "collectorId", userId.ToString() }
            };
            var activeSales = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("/api/sales/active", activeSalesParams);
            PendingAmount = activeSales?.Sum(s => s.Balance) ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
            // Fallback a valores por defecto si falla la API
            TodayPayments = 0;
            TodaySales = 0;
            PendingAmount = 0;
            TodayVisits = 0;
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System.Windows.Input;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class HomePage : ContentPage
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
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Inicializando HomePage ===");
            InitializeComponent();
            BindingContext = this;
            
            _apiService = new ApiService();
            
            GoToPaymentsCommand = new Command(async () => await Shell.Current.GoToAsync("//PaymentPage"));
            GoToSalesCommand = new Command(async () => await Shell.Current.GoToAsync("//SalesPage"));
            GoToCustomersCommand = new Command(async () => await Shell.Current.GoToAsync("//CustomersPage"));
            
            System.Diagnostics.Debug.WriteLine("HomePage: Comandos creados");
            
            _ = LoadDataAsync();
            
            System.Diagnostics.Debug.WriteLine("=== HomePage inicializado correctamente ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en HomePage constructor: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
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
            System.Diagnostics.Debug.WriteLine("HomePage: Cargando datos...");
            
            // Cargar datos del usuario desde SecureStorage
            Username = await SecureStorage.GetAsync("username") ?? "Usuario";
            Role = await SecureStorage.GetAsync("role") ?? "Cobrador";
            var userIdStr = await SecureStorage.GetAsync("user_id");
            int userId = int.TryParse(userIdStr, out var id) ? id : 0;
            
            System.Diagnostics.Debug.WriteLine($"HomePage: Usuario {Username}, Rol {Role}, ID {userId}");
            
            // Cargar estadísticas reales desde la API
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            
            // Obtener pagos de hoy del usuario
            var paymentsParams = new Dictionary<string, string?>
            {
                { "collectorId", userId.ToString() },
                { "startDate", today },
                { "endDate", today }
            };
            var paymentsToday = await _apiService.GetAsync<List<PaymentDto>>("api/payments", paymentsParams);
            TodayPayments = paymentsToday?.Where(p => p.Status == "approved").Sum(p => p.Amount) ?? 0;
            TodayVisits = paymentsToday?.Count ?? 0;
            
            System.Diagnostics.Debug.WriteLine($"HomePage: Pagos hoy: {TodayPayments}, Visitas: {TodayVisits}");
            
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
                var salesToday = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("api/sales", salesParams);
                TodaySales = salesToday?.Count ?? 0;
                
                System.Diagnostics.Debug.WriteLine($"HomePage: Ventas hoy: {TodaySales}");
            }
            
            // Obtener ventas activas asignadas al cobrador
            var activeSalesParams = new Dictionary<string, string?>
            {
                { "collectorId", userId.ToString() }
            };
            var activeSales = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("api/sales/active", activeSalesParams);
            PendingAmount = activeSales?.Sum(s => s.Balance) ?? 0;
            
            System.Diagnostics.Debug.WriteLine($"HomePage: Monto pendiente: {PendingAmount}");
            System.Diagnostics.Debug.WriteLine("HomePage: Datos cargados correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HomePage: Error loading data: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            // Fallback a valores por defecto si falla la API
            TodayPayments = 0;
            TodaySales = 0;
            PendingAmount = 0;
            TodayVisits = 0;
        }
    }
}

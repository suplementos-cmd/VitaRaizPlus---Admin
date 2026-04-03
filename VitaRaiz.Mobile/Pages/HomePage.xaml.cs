using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly PermissionsService _permissionsService;
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
            _permissionsService = IPlatformApplication.Current?.Services
                .GetService<PermissionsService>() ?? new PermissionsService(_apiService);
            
            System.Diagnostics.Debug.WriteLine("HomePage: Inicialización completa");
            
            _ = LoadDataAsync();
            
            System.Diagnostics.Debug.WriteLine("=== HomePage inicializado correctamente ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en HomePage constructor: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ══════════════════════════════════════════════════════════════════
    
    // ── Computed / display properties ──────────────────────────────
    public string UserInitials => string.IsNullOrWhiteSpace(_username) ? "?" :
        _username.Split(' ') is { Length: >= 2 } parts
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : _username.Length >= 2 ? _username.Substring(0, 2).ToUpper() : _username.ToUpper();

    public string RoleDisplayName => _role.ToLower() switch
    {
        var r when r.Contains("admin")      => "ADMINISTRADOR",
        var r when r.Contains("supervisor") => "SUPERVISORA",
        var r when r.Contains("vendedor")   => "VENDEDORA",
        var r when r.Contains("cobrador")   => "COBRADORA",
        _                                   => _role.ToUpper()
    };

    public string GreetingText
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var saludo = hour < 12 ? "Buenos días" : hour < 19 ? "Buenas tardes" : "Buenas noches";
            var firstName = _username.Split(' ')[0];
            return $"{saludo}, {firstName} 👋";
        }
    }

    public bool IsCobradorOrAdmin =>
        _role.Contains("cobrador",   StringComparison.OrdinalIgnoreCase) ||
        _role.Contains("admin",      StringComparison.OrdinalIgnoreCase) ||
        _role.Contains("supervisor", StringComparison.OrdinalIgnoreCase);

    public bool IsVendedorOrAbove =>
        _role.Contains("vendedor",   StringComparison.OrdinalIgnoreCase) ||
        _role.Contains("supervisor", StringComparison.OrdinalIgnoreCase) ||
        _role.Contains("admin",      StringComparison.OrdinalIgnoreCase);

    // ── Permission-based UI visibility (from API) ───────────────────
    // Show everything when service is null (XAML evaluates bindings before ctor assigns the service)
    // or when permissions haven't loaded yet (API offline / first launch).
    public bool CanSeeSalesButton     => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanViewOwnSales;
    public bool CanSeeCustomersButton => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanViewCustomers;
    public bool CanCreateSaleButton   => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanCreateSale;
    public bool CanSeePaymentsStats   => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanViewPayments;
    public bool CanSeeReports         => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanViewReports;

    // HomePage is a dashboard — search is not applicable here.
    public bool HasSearchableData => false;
    public string SearchText { get; set; } = string.Empty;

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UserInitials));
            OnPropertyChanged(nameof(GreetingText));
        }
    }

    public string Role
    {
        get => _role;
        set
        {
            _role = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RoleDisplayName));
            OnPropertyChanged(nameof(IsCobradorOrAdmin));
            OnPropertyChanged(nameof(IsVendedorOrAbove));
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

            // Cargar permisos (si aún no están disponibles)
            await _permissionsService.LoadAsync();
            NotifyPermissions();
            
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

    private async Task OnGoToSales()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== OnGoToSales START ===");
            await Shell.Current.GoToAsync("//SalesPage");
            System.Diagnostics.Debug.WriteLine("=== Navegación a SalesPage completada ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnGoToSales: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private void NotifyPermissions()
    {
        OnPropertyChanged(nameof(CanSeeSalesButton));
        OnPropertyChanged(nameof(CanSeeCustomersButton));
        OnPropertyChanged(nameof(CanCreateSaleButton));
        OnPropertyChanged(nameof(CanSeePaymentsStats));
        OnPropertyChanged(nameof(CanSeeReports));
    }

    private async void OnSalesClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OnSalesClicked EVENT ===");
        await OnGoToSales();
    }

    private async Task OnGoToCustomers()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== OnGoToCustomers START ===");
            await Shell.Current.GoToAsync("//CustomersPage");
            System.Diagnostics.Debug.WriteLine("=== Navegación a CustomersPage completada ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnGoToCustomers: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private async void OnCustomersClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OnCustomersClicked EVENT ===");
        await OnGoToCustomers();
    }

    private async void OnNuevaVentaClicked(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== OnNuevaVentaClicked ===");
            await Shell.Current.GoToAsync("CreateSalePage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnNuevaVentaClicked: {ex.Message}");
        }
    }

    
    // ══════════════════════════════════════════════════════════════════
    // CERRAR SESIÓN
    // ══════════════════════════════════════════════════════════════════
    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlert(
                "Cerrar Sesión", 
                "¿Está seguro que desea cerrar sesión?", 
                "Sí", 
                "No"
            );
            
            if (!confirm) return;
            
            System.Diagnostics.Debug.WriteLine("=== Cerrando sesión ===");
            
            // IMPORTANTE: Resetear tema ANTES de limpiar storage
            App.ResetThemeToDefault();
            ApiService.ClearCachedToken();
            Services.PageDataCache.PrefetchedSales     = null;
            Services.PageDataCache.PrefetchedCustomers = null;

            // Limpiar permisos cacheados
            var permSvc = IPlatformApplication.Current?.Services.GetService<PermissionsService>();
            permSvc?.Clear();
            
            // Limpiar credenciales almacenadas
            SecureStorage.Remove("auth_token");
            SecureStorage.Remove("username");
            SecureStorage.Remove("role");
            SecureStorage.Remove("user_id");
            SecureStorage.RemoveAll();
            
            System.Diagnostics.Debug.WriteLine("Credenciales eliminadas del SecureStorage");
            
            // Cambiar la MainPage a LoginPage
            Application.Current!.MainPage = new LoginPage();
            
            System.Diagnostics.Debug.WriteLine("MainPage cambiada a LoginPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnLogoutTapped: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            await DisplayAlert("Error", "No se pudo cerrar la sesión", "OK");
        }
    }

    private async void OnRefreshTapped(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Actualizando datos de HomePage ===");
            await LoadDataAsync();
            await DisplayAlert("Actualizado", "Los datos se han actualizado correctamente", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnRefreshTapped: {ex.Message}");
            await DisplayAlert("Error", "No se pudo actualizar los datos", "OK");
        }
    }
}

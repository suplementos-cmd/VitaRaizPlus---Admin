using VitaRaiz.Mobile.Pages.HomePartials;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly PermissionsService _permissionsService;
    private string _username = string.Empty;
    private string _role = string.Empty;
    private HomeContext? _context;

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

    // HomePage has no search — binding required by PageFrame.
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
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("HomePage: Cargando datos...");

            // ── Leer identidad del usuario ────────────────────────────────────
            Username = await SecureStorage.GetAsync("username") ?? "Usuario";
            Role     = await SecureStorage.GetAsync("role")     ?? "cobrador";
            var userIdStr = await SecureStorage.GetAsync("user_id");
            int userId = int.TryParse(userIdStr, out var id) ? id : 0;

            System.Diagnostics.Debug.WriteLine($"HomePage: Usuario={Username}, Rol={Role}, ID={userId}");

            // ── Cargar permisos ───────────────────────────────────────────────
            await _permissionsService.LoadAsync();

            // ── Crear contexto compartido (solo en la primera carga) ──────────
            if (_context == null)
            {
                _context = new HomeContext
                {
                    UserId      = userId,
                    Username    = _username,
                    Role        = _role,
                    Api         = _apiService,
                    Permissions = _permissionsService,

                    GoToSales         = async () => await Shell.Current.GoToAsync("//SalesPage"),
                    GoToCustomers     = async () => await Shell.Current.GoToAsync("//CustomersPage"),
                    GoToCreateSale    = async () => await Shell.Current.GoToAsync("CreateSalePage"),
                    GoToRegistroCobro = async () => await Shell.Current.GoToAsync("//SalesPage"),
                };

                // Instanciar la vista parcial del rol y montarla en el host
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (this.FindByName<ContentView>("RolePartialHost") is { } host)
                        host.Content = CreateRolePartial(_role, _context);
                });
            }

            // ── Cargar estadísticas base (se actualizan en cada refresh) ──────
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            var paymentsParams = new Dictionary<string, string?>
            {
                { "collectorId", userId.ToString() },
                { "startDate",   today },
                { "endDate",     today },
            };
            var paymentsToday = await _apiService.GetAsync<List<PaymentDto>>("api/payments", paymentsParams);
            _context.TodayPayments = paymentsToday?.Where(p => p.Status == "approved").Sum(p => p.Amount) ?? 0;
            _context.TodayVisits   = paymentsToday?.Count ?? 0;

            if (IsVendedorOrSupervisorOrAdmin(_role))
            {
                var salesParams = new Dictionary<string, string?>
                {
                    { "sellerId",   userId.ToString() },
                    { "startDate", today },
                    { "endDate",   today },
                };
                var salesToday = await _apiService.GetAsync<List<SaleDto>>("api/sales", salesParams);
                _context.TodaySales = salesToday?.Count ?? 0;
            }

            var activeSalesParams = new Dictionary<string, string?> { { "collectorId", userId.ToString() } };
            var activeSales = await _apiService.GetAsync<List<SaleDto>>("api/sales/active", activeSalesParams);
            _context.PendingAmount = activeSales?.Sum(s => s.Balance) ?? 0;

            System.Diagnostics.Debug.WriteLine(
                $"HomePage: Cobros={_context.TodayPayments}, Ventas={_context.TodaySales}, Pendiente={_context.PendingAmount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HomePage: Error loading data: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ── Role → partial view factory ───────────────────────────────────────
    private static ContentView CreateRolePartial(string role, HomeContext ctx)
    {
        bool hasWord(string keyword) => role.Contains(keyword, StringComparison.OrdinalIgnoreCase);

        if (hasWord("admin") && hasWord("rh"))     return new HomeAdminRHView(ctx);
        if (hasWord("admin"))                      return new HomeAdminFullView(ctx);
        if (hasWord("supervisor") && hasWord("cobro")) return new HomeSupervisorCobrosView(ctx);
        if (hasWord("supervisor"))                 return new HomeSupervisoraVentasView(ctx);
        if (hasWord("cobrador"))                   return new HomeCobradorView(ctx);
        return new HomeVendedorView(ctx);   // default: vendedor
    }

    private static bool IsVendedorOrSupervisorOrAdmin(string role)
        => role.Contains("vendedor",   StringComparison.OrdinalIgnoreCase)
        || role.Contains("supervisor", StringComparison.OrdinalIgnoreCase)
        || role.Contains("admin",      StringComparison.OrdinalIgnoreCase);

    
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

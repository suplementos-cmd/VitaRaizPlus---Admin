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
    private string _searchText = string.Empty;

    public HomePage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Inicializando HomePage ===");
            InitializeComponent();
            BindingContext = this;
            
            _apiService = new ApiService();
            
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
            OnPropertyChanged(nameof(HasSearchableData));
        }
    }

    public int TodaySales
    {
        get => _todaySales;
        set
        {
            _todaySales = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSearchableData));
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
            OnPropertyChanged(nameof(HasSearchableData));
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Habilita el botón de búsqueda solo cuando hay datos para buscar
    /// </summary>
    public bool HasSearchableData => TodaySales > 0 || TodayVisits > 0 || TodayPayments > 0;
    
    public double SearchIconOpacity => HasSearchableData ? 1.0 : 0.5;

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
            
            // ══════════════════════════════════════════════════════════════════
            // LOAD DYNAMIC THEME FROM API/Profile
            // ══════════════════════════════════════════════════════════════════
            await LoadUserThemeAsync(userId, Role);
            
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

    // ══════════════════════════════════════════════════════════════════
    // LOAD DYNAMIC THEME - Aplicar globalmente a todas las páginas
    // ══════════════════════════════════════════════════════════════════
    private async Task LoadUserThemeAsync(int userId, string role)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoadUserTheme] Cargando tema para usuario {userId}, rol {role}...");
            
            string themeColor;
            string themeColorLight;
            string themeColorLighter;
            
            // Intentar cargar configuración de tema desde la API
            try
            {
                var userSettings = await _apiService.GetAsync<UserSettingsDto>($"api/users/{userId}/settings");
                
                if (userSettings != null && !string.IsNullOrEmpty(userSettings.ThemeColor))
                {
                    themeColor = userSettings.ThemeColor;
                    themeColorLight = userSettings.ThemeColorLight ?? LightenColor(themeColor, 0.3);
                    themeColorLighter = userSettings.ThemeColorLighter ?? LightenColor(themeColor, 0.6);
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadUserTheme] Tema cargado desde API: {themeColor}");
                }
                else
                {
                    // Tema por defecto según rol
                    themeColor = GetThemeByRole(role);
                    themeColorLight = LightenColor(themeColor, 0.3);
                    themeColorLighter = LightenColor(themeColor, 0.6);
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadUserTheme] Tema por defecto según rol: {themeColor}");
                }
            }
            catch
            {
                // En caso de error, usar tema por defecto
                themeColor = GetThemeByRole(role);
                themeColorLight = LightenColor(themeColor, 0.3);
                themeColorLighter = LightenColor(themeColor, 0.6);
            }
            
            // Aplicar tema GLOBALMENTE para todas las páginas
            App.UpdateThemeColors(themeColor, themeColorLight, themeColorLighter);
            
            System.Diagnostics.Debug.WriteLine($"[LoadUserTheme] Tema aplicado globalmente: {themeColor}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadUserTheme] Error: {ex.Message}");
        }
    }

    private string GetThemeByRole(string role)
    {
        return role.ToLower() switch
        {
            var r when r.Contains("admin") => "#E91E63", // Rosa para admin
            var r when r.Contains("supervisor") => "#FF9800", // Naranja para supervisor
            var r when r.Contains("vendedor") => "#2196F3", // Azul para vendedor
            var r when r.Contains("cobrador") => "#28A745", // Verde para cobrador
            _ => "#28A745" // Verde por defecto
        };
    }

    /// <summary>
    /// Aclara un color hex agregando transparencia o mezclando con blanco
    /// </summary>
    private string LightenColor(string hexColor, double factor)
    {
        try
        {
            // Remover el # si existe
            hexColor = hexColor.TrimStart('#');
            
            // Convertir a RGB
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            
            // Aclarar mezclando con blanco
            r = (int)(r + (255 - r) * factor);
            g = (int)(g + (255 - g) * factor);
            b = (int)(b + (255 - b) * factor);
            
            // Retornar color aclarado
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return hexColor;
        }
    }

    private async Task OnGoToPayments()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== OnGoToPayments START ===");
            await Shell.Current.GoToAsync("//PaymentPage");
            System.Diagnostics.Debug.WriteLine("=== Navegación a PaymentPage completada ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnGoToPayments: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private async void OnPaymentsClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OnPaymentsClicked EVENT ===");
        await OnGoToPayments();
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

    private async void OnSalesClicked(object sender, EventArgs e)
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

    private async void OnCustomersClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OnCustomersClicked EVENT ===");
        await OnGoToCustomers();
    }

    // ═══ Bottom Tab Navigation ═══
    private async void OnTabVentas(object? s, EventArgs e) => await Shell.Current.GoToAsync("//SalesPage");
    private async void OnTabCobranza(object? s, EventArgs e) => await Shell.Current.GoToAsync("//PaymentPage");
    private async void OnTabClientes(object? s, EventArgs e) => await Shell.Current.GoToAsync("//CustomersPage");
    
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

    // ══════════════════════════════════════════════════════════════════
    // HEADER ACTIONS
    // ══════════════════════════════════════════════════════════════════
    private void OnSearchToggle(object? sender, EventArgs e)
    {
        if (!HasSearchableData)
        {
            System.Diagnostics.Debug.WriteLine("[OnSearchToggle] No hay datos para buscar");
            return;
        }
        
        // Toggle la barra de búsqueda compacta dentro del header
        SearchBarCompact.IsVisible = !SearchBarCompact.IsVisible;
        System.Diagnostics.Debug.WriteLine($"[OnSearchToggle] SearchBar visible: {SearchBarCompact.IsVisible}");
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

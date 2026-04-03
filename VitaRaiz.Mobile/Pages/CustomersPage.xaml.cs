using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;
using NLog;

namespace VitaRaiz.Mobile.Pages;

public partial class CustomersPage : ContentPage
{
    private readonly Logger _logger = AppLogger.Get();
    private readonly ApiService _apiService;
    private readonly PermissionsService _permissionsService;
    private string _searchText = string.Empty;
    private List<CustomerDto> _allCustomers = new();

    public CustomersPage()
    {
        try
        {
            _logger.Info("═══════════════════════════════════════════════════════");
            _logger.Info("[Constructor] Inicializando CustomersPage...");
            InitializeComponent();
            BindingContext = this;
            
            _apiService = new ApiService();
            _permissionsService = Application.Current?.Handler?.MauiContext?.Services
                .GetService<PermissionsService>() ?? new PermissionsService(_apiService);
            
            SearchCommand = new Command(OnSearch);
            ViewCustomerDetailCommand = new Command(OnViewCustomerDetail);
            
            _ = LoadCustomersAsync();
            _logger.Info("[Constructor] CustomersPage inicializado correctamente");
            _logger.Info("═══════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error crítico en constructor CustomersPage");
            throw;
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

    public ObservableCollection<CustomerItemDto> Customers { get; set; } = new();

    /// <summary>
    /// Habilita el botón de búsqueda solo cuando hay clientes
    /// </summary>
    public bool HasCustomers => _allCustomers.Count > 0;
    
    public double SearchIconOpacity => HasCustomers ? 1.0 : 0.5;

    // ── Permission-based UI visibility ──────────────────────────────
    public bool CanCreateCustomer => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanCreateCustomer;
    public bool CanEditCustomer   => _permissionsService is null || !_permissionsService.IsLoaded || _permissionsService.CanEditCustomer;

    public ICommand SearchCommand { get; }
    public ICommand ViewCustomerDetailCommand { get; }

    private async Task LoadCustomersAsync()
    {
        try
        {
            // Asegurar permisos cargados
            await _permissionsService.LoadAsync();
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanEditCustomer));

            _logger.Info("[LoadCustomersAsync] INICIO - Cargando clientes desde API...");
            
            // Cargar clientes desde la API (o desde el prefetch del login si está disponible)
            var customersData = Services.PageDataCache.PrefetchedCustomers as List<CustomerDto>;
            if (customersData != null)
            {
                Services.PageDataCache.PrefetchedCustomers = null;
                _logger.Info("[LoadCustomersAsync] Using prefetched data ({Count} clientes)", customersData.Count);
            }
            else
            {
                customersData = await _apiService.GetAsync<List<CustomerDto>>("api/customers");
            }
            
            _logger.Info("[LoadCustomersAsync] API Response recibida: Count={Count}, IsNull={IsNull}", 
                customersData?.Count ?? 0, customersData == null);
            
            if (customersData != null)
            {
                _allCustomers = customersData;
                _logger.Debug("[LoadCustomersAsync] Datos asignados a _allCustomers. Llamando DisplayCustomers...");
                
                DisplayCustomers(_allCustomers);
                
                // Notificar cambios en propiedades dependientes
                OnPropertyChanged(nameof(HasCustomers));
                OnPropertyChanged(nameof(SearchIconOpacity));
                
                _logger.Info("[LoadCustomersAsync] FIN - {Count} clientes cargados y mostrados en UI", Customers.Count);
            }
            else
            {
                _logger.Warn("[LoadCustomersAsync] API devolvió null - No hay datos de clientes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error crítico al cargar clientes desde API");
        }
    }

    private void DisplayCustomers(List<CustomerDto> customers)
    {
        _logger.Debug("[DisplayCustomers] INICIO - Procesando {Count} clientes", customers.Count);
        
        var colors = new[] { "#9C27B0", "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#00BCD4" };
        int colorIndex = 0;
        
        var items = new List<CustomerItemDto>();
        foreach (var customer in customers)
        {
            var item = new CustomerItemDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                PhoneNumber = customer.PhoneNumber ?? "Sin teléfono",
                ZoneName = customer.ZoneName ?? "Sin zona",
                PendingBalance = 0, // TODO: Obtener balance pendiente del cliente
                IsGoldCustomer = customer.IsGoldCustomer,
                IsBlacklisted = customer.IsBlacklisted,
                AvatarColor = colors[colorIndex % colors.Length]
            };
            items.Add(item);
            
            if (colorIndex < 3) // Log primeros 3 para muestra
                _logger.Debug("[DisplayCustomers] Item {Index}: Id={Id}, Name={Name}, Zone={Zone}",
                    colorIndex, item.CustomerId, item.CustomerName, item.ZoneName);
            
            colorIndex++;
        }
        
        _logger.Debug("[DisplayCustomers] {Count} items procesados. Actualizando UI en MainThread...", items.Count);
        
        // CRITICAL: Modify ObservableCollection only on UI thread to prevent crash 0xc000027b
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                _logger.Debug("[DisplayCustomers] En UI Thread. Limpiando Customers ObservableCollection...");
                Customers.Clear();
                
                _logger.Debug("[DisplayCustomers] Agregando {Count} items a Customers...", items.Count);
                foreach (var item in items)
                    Customers.Add(item);
                
                _logger.Info("[DisplayCustomers] FIN - UI actualizada con {Count} clientes", Customers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "CRASH al actualizar ObservableCollection Customers en UI thread");
                throw;
            }
        });
    }

    private void OnSearch()
    {
        _logger.Info("[OnSearch] Búsqueda solicitada: SearchText='{SearchText}'", SearchText ?? "(vacío)");
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _logger.Debug("[OnSearch] Búsqueda vacía - Mostrando todos los clientes ({Count})", _allCustomers.Count);
            DisplayCustomers(_allCustomers);
        }
        else
        {
            var filtered = _allCustomers.Where(c => 
                c.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(SearchText))
            ).ToList();
            
            _logger.Info("[OnSearch] Filtro aplicado: {FilteredCount} de {TotalCount} clientes", 
                filtered.Count, _allCustomers.Count);
            
            DisplayCustomers(filtered);
        }
    }

    private void OnViewCustomerDetail()
    {
        // TODO: Navegar a detalle de cliente
    }

    // ══════════════════════════════════════════════════════════════════
    // HEADER ACTIONS
    // ══════════════════════════════════════════════════════════════════

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        OnSearch();
    }

    private async void OnRefreshTapped(object? sender, EventArgs e)
    {
        try
        {
            _logger.Info("[OnRefreshTapped] Actualizando lista de clientes...");
            await LoadCustomersAsync();
            await DisplayAlert("Actualizado", "La lista de clientes se ha actualizado correctamente", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error al actualizar clientes");
            await DisplayAlert("Error", "No se pudo actualizar la lista de clientes", "OK");
        }
    }

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
            
            _logger.Info("[OnLogoutTapped] Cerrando sesión...");
            
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
            
            // Cambiar la MainPage a LoginPage
            Application.Current!.MainPage = new LoginPage();
            
            _logger.Info("[OnLogoutTapped] Sesión cerrada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error al cerrar sesión");
            await DisplayAlert("Error", "No se pudo cerrar la sesión", "OK");
        }
    }

}

public class CustomerItemDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public decimal PendingBalance { get; set; }
    public bool IsGoldCustomer { get; set; }
    public bool IsBlacklisted { get; set; }
    public string AvatarColor { get; set; } = "#999";
    public string Initials => CustomerName.Length >= 2 ? CustomerName.Substring(0, 2).ToUpper() : "XX";
    public string StatusIcon => IsBlacklisted ? "⛔" : IsGoldCustomer ? "⭐" : "👤";
}

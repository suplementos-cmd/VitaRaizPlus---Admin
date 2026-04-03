using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;
    // URL de la API - En desarrollo usa dotnet run (http) profile
    // Para Windows Machine usa localhost, para Android usa 10.0.2.2
#if WINDOWS
    private const string API_BASE_URL = "http://localhost:5299"; // dotnet run --launch-profile http
#else
    private const string API_BASE_URL = "http://10.0.2.2:5299"; // Android emulator -> host machine
#endif

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasError;

    public LoginPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Inicializando LoginPage ===");
            
            InitializeComponent();
            
            System.Diagnostics.Debug.WriteLine("InitializeComponent OK");
            
            // Configurar HttpClient con timeout más largo y bypass de SSL para desarrollo
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(API_BASE_URL),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            System.Diagnostics.Debug.WriteLine($"HttpClient configurado. BaseAddress: {_httpClient.BaseAddress}");
            
            // IMPORTANTE: Crear el LoginCommand ANTES de asignar BindingContext
            LoginCommand = new Command(async () => await OnLoginClicked(), () => IsNotLoading);
            
            System.Diagnostics.Debug.WriteLine($"LoginCommand creado. IsNotLoading: {IsNotLoading}");
            
            BindingContext = this;
            
            System.Diagnostics.Debug.WriteLine("BindingContext asignado");
            
            // Log para debugging
            System.Diagnostics.Debug.WriteLine($"LoginPage: API_BASE_URL = {API_BASE_URL}");
            System.Diagnostics.Debug.WriteLine("=== LoginPage inicializado correctamente ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en constructor LoginPage: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
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

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotLoading));
            
            // Actualizar el estado del botón directamente
            if (LoginButton != null)
            {
                LoginButton.IsEnabled = !value;
            }
            
            if (LoginCommand != null && LoginCommand is Command command)
            {
                command.ChangeCanExecute();
            }
        }
    }

    public bool IsNotLoading => !IsLoading;

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoginCommand { get; }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== LoginButton_Clicked EVENT HANDLER ===");
        await OnLoginClicked();
    }

    private async Task OnLoginClicked()
    {
        System.Diagnostics.Debug.WriteLine($"=== OnLoginClicked START ===");
        
        // Validaciones
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            System.Diagnostics.Debug.WriteLine("Validación fallida: campos vacíos");
            ErrorMessage = "Por favor, ingresa usuario y contraseña";
            HasError = true;
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Validación OK. Usuario: {Username}");
        
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Intentando login ===");
            System.Diagnostics.Debug.WriteLine($"Usuario: {Username}");
            System.Diagnostics.Debug.WriteLine($"API URL: {_httpClient.BaseAddress}api/Auth/login");
            
            // Preparar request
            var loginRequest = new
            {
                username = Username,
                password = Password
            };

            var jsonContent = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            System.Diagnostics.Debug.WriteLine($"JSON Request: {jsonContent}");

            // Llamar API
            System.Diagnostics.Debug.WriteLine("Enviando request...");
            var response = await _httpClient.PostAsync("api/Auth/login", content);
            System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");
                
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    System.Diagnostics.Debug.WriteLine("Login exitoso — iniciando carga paralela...");

                    // 1. Caché de token en memoria de inmediato — todas las llamadas API siguientes
                    //    usan esta caché sin leer SecureStorage (evita ~200ms por llamada en Windows).
                    ApiService.SetCachedToken(loginResponse.Token);

                    var themeService       = IPlatformApplication.Current.Services.GetRequiredService<ThemeService>();
                    var catalogService     = IPlatformApplication.Current.Services.GetRequiredService<CatalogService>();
                    var permissionsService = IPlatformApplication.Current.Services.GetRequiredService<PermissionsService>();

                    // 2. Ejecutar en paralelo todo lo que no tiene dependencias entre sí:
                    //    • Escrituras a SecureStorage (5 escrituras simultaneas en lugar de secuenciales)
                    //    • Carga de catálogos desde API
                    //    • Carga de permisos desde API
                    await Task.WhenAll(
                        // SecureStorage: 5 escrituras simultáneas
                        Task.WhenAll(
                            SecureStorage.SetAsync("auth_token", loginResponse.Token),
                            SecureStorage.SetAsync("user_id",    loginResponse.UserId.ToString()),
                            SecureStorage.SetAsync("username",   loginResponse.Username),
                            SecureStorage.SetAsync("role",       loginResponse.Role),
                            SecureStorage.SetAsync("role_id",    loginResponse.RoleId.ToString())),
                        // API: catálogos y permisos en paralelo
                        catalogService.LoadAsync(forceReload: true),
                        permissionsService.LoadAsync(forceReload: true));

                    // 3. Tema — llama directo a GET api/Catalogs/theme/role/{roleId} (endpoint dedicado por perfil).
                    await themeService.LoadAndApplyAsync(loginResponse.RoleId);

                    // 4. Prefetch en background — no bloquea la navegación. Mientras el usuario
                    //    ve el HomePage, ventas y clientes se cargan en segundo plano. Cuando el
                    //    usuario toca esas pestañas, los datos ya están listos → 0 spinner.
                    _ = PrefetchPageDataAsync();

                    System.Diagnostics.Debug.WriteLine("[Login] Carga paralela completada. Navegando a AppShell...");

                    // 5. Navegar al AppShell con el tema ya aplicado — sin flash de color.
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            var window = Microsoft.Maui.Controls.Application.Current?.Windows[0];
                            if (window != null)
                            {
                                window.Page = new AppShell();
                                System.Diagnostics.Debug.WriteLine("[Login] Navegación completada!");
                            }
                            else
                            {
                                ErrorMessage = "Error al navegar: Window no disponible";
                                HasError = true;
                            }
                        }
                        catch (Exception navEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Login] ERROR al navegar: {navEx.Message}");
                            ErrorMessage = $"Error al navegar: {navEx.Message}";
                            HasError = true;
                        }
                    });
                }
                else
                {
                    ErrorMessage = "Respuesta inválida del servidor";
                    HasError = true;
                    System.Diagnostics.Debug.WriteLine("ERROR: Respuesta del servidor no contiene token válido");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error Content: {errorContent}");
                
                ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Usuario o contraseña incorrectos"
                    : $"Error al iniciar sesión: {response.StatusCode}";
                HasError = true;
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Error de conexión: {ex.Message}";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"HttpRequestException: {ex}");
            System.Diagnostics.Debug.WriteLine($"Intentando conectar a: {API_BASE_URL}");
        }
        catch (TaskCanceledException ex)
        {
            ErrorMessage = "Tiempo de espera agotado. Verifica que la API esté corriendo.";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"TaskCanceledException: {ex}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Prefetch sales and customers data in background during login.
    /// Results are stored in PageDataCache so SalesPage and CustomersPage render instantly.
    /// </summary>
    private static async Task PrefetchPageDataAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[Prefetch] Iniciando precarga de ventas y clientes...");
            var api = new ApiService();
            var salesTask     = api.GetAsync<List<SaleDto>>("api/sales");
            var customersTask = api.GetAsync<List<CustomerDto>>("api/customers");
            await Task.WhenAll(salesTask, customersTask);
            PageDataCache.PrefetchedSales     = salesTask.Result;
            PageDataCache.PrefetchedCustomers = customersTask.Result;
            System.Diagnostics.Debug.WriteLine(
                $"[Prefetch] Completado \u2014 ventas={PageDataCache.PrefetchedSales?.Count ?? 0}, " +
                $"clientes={PageDataCache.PrefetchedCustomers?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            // Non-fatal: pages will load data normally on first visit if prefetch fails.
            System.Diagnostics.Debug.WriteLine($"[Prefetch] No fatal: {ex.Message}");
        }
    }
}

// DTOs
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
}


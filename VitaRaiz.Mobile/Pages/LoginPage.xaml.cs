using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace VitaRaiz.Mobile.Pages;

public partial class LoginPage : ContentPage, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://localhost:7001/api"; // TODO: Cambiar a URL de producción

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasError;

    public LoginPage()
    {
        InitializeComponent();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(API_BASE_URL)
        };
        
        BindingContext = this;
        
        LoginCommand = new Command(async () => await OnLoginClicked(), () => IsNotLoading);
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
            ((Command)LoginCommand).ChangeCanExecute();
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

    private async Task OnLoginClicked()
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Por favor, ingresa usuario y contraseña";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            // Preparar request
            var loginRequest = new
            {
                username = Username,
                password = Password
            };

            var jsonContent = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Llamar API
            var response = await _httpClient.PostAsync("/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    // Guardar token en SecureStorage
                    await SecureStorage.SetAsync("jwt_token", loginResponse.Token);
                    await SecureStorage.SetAsync("user_id", loginResponse.UserId.ToString());
                    await SecureStorage.SetAsync("username", loginResponse.Username);
                    await SecureStorage.SetAsync("role", loginResponse.Role);

                    // Navegar a la página principal
                    MauiApp.Current.MainPage = new AppShell();
                }
                else
                {
                    ErrorMessage = "Respuesta inválida del servidor";
                    HasError = true;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Usuario o contraseña incorrectos"
                    : $"Error al iniciar sesión: {response.StatusCode}";
                HasError = true;
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = "Error de conexión. Verifica tu internet o que la API esté disponible.";
            HasError = true;
            Console.WriteLine($"HttpRequestException: {ex.Message}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error inesperado: {ex.Message}";
            HasError = true;
            Console.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// DTOs
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

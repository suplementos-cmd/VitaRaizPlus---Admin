using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VitaRaiz.WebPortal.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const string TOKEN_KEY = "authToken";
    private const string USER_KEY = "currentUser";

    public event EventHandler? AuthenticationStateChanged;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        };
    }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new { username, password };
            var jsonContent = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    // Guardar token y usuario en sessionStorage (se implementará en el componente)
                    var authResponse = new AuthResponse
                    {
                        Success = true,
                        Token = loginResponse.Token,
                        UserId = loginResponse.UserId,
                        Username = loginResponse.Username,
                        Role = loginResponse.Role,
                        Message = "Login exitoso"
                    };

                    AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
                    return authResponse;
                }
            }

            return new AuthResponse
            {
                Success = false,
                Message = "Usuario o contraseña incorrectos"
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Error de conexión: {ex.Message}"
            };
        }
    }

    public void Logout()
    {
        AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync("/api/auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CurrentUser
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
}

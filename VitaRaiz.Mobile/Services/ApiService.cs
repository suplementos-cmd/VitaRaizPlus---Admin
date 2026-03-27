using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NLog;

namespace VitaRaiz.Mobile.Services;

public class ApiService
{
    private readonly Logger _logger = AppLogger.Get();
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // URL de la API - En desarrollo usa dotnet run (http) profile
#if WINDOWS
    private const string API_BASE_URL = "http://localhost:5299"; // dotnet run --launch-profile http
#else
    private const string API_BASE_URL = "http://10.0.2.2:5299"; // Android emulator -> host machine
#endif

    public ApiService()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(API_BASE_URL),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        _logger.Info("[ApiService] Initialized with API_BASE_URL: {BaseUrl}", API_BASE_URL);
        System.Diagnostics.Debug.WriteLine($"ApiService: API_BASE_URL = {API_BASE_URL}");
    }

    private async Task SetAuthorizationHeaderAsync()
    {
        try
        {
#if IOS || MACCATALYST || ANDROID || WINDOWS
            // Llamada a SecureStorage solo compilada en plataformas que la soportan.
            var token = await SecureStorage.GetAsync("auth_token");
            _logger.Debug("[SetAuthorizationHeaderAsync] Token retrieved from storage: {HasToken}", !string.IsNullOrEmpty(token));
            System.Diagnostics.Debug.WriteLine($"[ApiService] Token from storage: {(string.IsNullOrEmpty(token) ? "NULL/EMPTY" : token.Substring(0, Math.Min(50, token.Length)))}...");
            
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.Debug("[SetAuthorizationHeaderAsync] Authorization header set successfully");
                System.Diagnostics.Debug.WriteLine($"[ApiService] Authorization header set successfully");
            }
            else
            {
                _logger.Warn("[SetAuthorizationHeaderAsync] No token found in SecureStorage");
                System.Diagnostics.Debug.WriteLine($"[ApiService] WARNING: No token found in SecureStorage!");
            }
#else
            // En plataformas que no soportan SecureStorage, omitir la lectura del token.
            _logger.Warn("[SetAuthorizationHeaderAsync] SecureStorage no está soportado en esta plataforma. Se omite lectura del token.");
            System.Diagnostics.Debug.WriteLine("[ApiService] WARNING: SecureStorage not supported on this platform. Skipping auth header.");
            return;
#endif
        }
        catch (PlatformNotSupportedException pnse)
        {
            // Protección adicional por si la API lanza PlatformNotSupported en tiempo de ejecución
            _logger.Warn(pnse, "[SetAuthorizationHeaderAsync] SecureStorage no está soportado en esta plataforma (excepción).");
            System.Diagnostics.Debug.WriteLine($"[ApiService] SecureStorage not supported: {pnse.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[SetAuthorizationHeaderAsync] Error setting auth header");
            System.Diagnostics.Debug.WriteLine($"[ApiService] ERROR in SetAuthorizationHeaderAsync: {ex}");
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        _logger.Info("[GetAsync] Starting GET {Endpoint}", endpoint);
        try
        {
            await SetAuthorizationHeaderAsync();
            System.Diagnostics.Debug.WriteLine($"[ApiService] GET {endpoint}");
            var response = await _httpClient.GetAsync(endpoint);
            
            _logger.Debug("[GetAsync] Response Status: {StatusCode}", response.StatusCode);
            System.Diagnostics.Debug.WriteLine($"[ApiService] Response Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.Debug("[GetAsync] Response Content Length: {Length}", content?.Length ?? 0);
                System.Diagnostics.Debug.WriteLine($"[ApiService] Response Content Length: {content?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[ApiService] Response Content: {content?.Substring(0, Math.Min(200, content?.Length ?? 0))}");
                
                System.Diagnostics.Debug.WriteLine($"[ApiService] Starting deserialization to {typeof(T).Name}...");
                var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                System.Diagnostics.Debug.WriteLine($"[ApiService] Deserialized result is null: {result == null}");
                
                if (result != null && result is System.Collections.ICollection collection)
                {
                    _logger.Info("[GetAsync] Deserialized collection: Count={Count}", collection.Count);
                    System.Diagnostics.Debug.WriteLine($"[ApiService] Deserialized collection has {collection.Count} items");
                }
                
                _logger.Info("[GetAsync] Returning result from GET {Endpoint}", endpoint);
                System.Diagnostics.Debug.WriteLine($"[ApiService] Returning result from GetAsync<{typeof(T).Name}>");
                return result;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.Error("[GetAsync] GET {Endpoint} failed: Status={StatusCode}, Body={ErrorBody}", 
                endpoint, response.StatusCode, errorContent?.Substring(0, Math.Min(200, errorContent?.Length ?? 0)));
            Console.WriteLine($"GET {endpoint} failed with status: {response.StatusCode}, Body: {errorContent}");
            return default;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[GetAsync] Exception in GET {Endpoint}", endpoint);
            System.Diagnostics.Debug.WriteLine($"[ApiService] Exception: {ex.ToString()}");
            return default;
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string?> queryParams)
    {
        var queryString = BuildQueryString(queryParams);
        var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
        return await GetAsync<T>(fullEndpoint);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine($"[ApiService] POST {endpoint} - Body: {jsonContent}");
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ApiService] POST {endpoint} - Status: {response.StatusCode}");
            Console.WriteLine($"[ApiService] POST {endpoint} - Response: {responseContent}");
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            }
            
            Console.WriteLine($"[ApiService] POST {endpoint} FAILED: {response.StatusCode} - {responseContent}");
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] POST {endpoint} EXCEPTION: {ex.Message}");
            Console.WriteLine($"[ApiService] StackTrace: {ex.StackTrace}");
            return default;
        }
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en POST {endpoint}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> PostMultipartAsync(string endpoint, MultipartFormDataContent content)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en POST multipart {endpoint}: {ex.Message}");
            return false;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            }
            
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en PUT {endpoint}: {ex.Message}");
            return default;
        }
    }

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en PUT {endpoint}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en DELETE {endpoint}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            var current = Connectivity.Current.NetworkAccess;
            if (current != NetworkAccess.Internet)
                return false;

            // Verificar conectividad con el servidor
            var response = await _httpClient.GetAsync("/health", new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var queryParams = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        
        return string.Join("&", queryParams);
    }
}

// DTOs para mapear las respuestas de la API
public class SaleDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DateTime SaleDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentTerms { get; set; }
}

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int SaleId { get; set; }
    public int CollectorId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Validation { get; set; }
    public string? Notes { get; set; }
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public bool IsGoldCustomer { get; set; }
    public bool IsBlacklisted { get; set; }
    public DateTime CreatedAt { get; set; }
}

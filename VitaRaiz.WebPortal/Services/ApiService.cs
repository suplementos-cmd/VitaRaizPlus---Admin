using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace VitaRaiz.WebPortal.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(IConfiguration configuration, ProtectedSessionStorage sessionStorage)
    {
        _configuration = configuration;
        _sessionStorage = sessionStorage;
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task SetAuthorizationHeaderAsync()
    {
        try
        {
            var userResult = await _sessionStorage.GetAsync<CurrentUser>("currentUser");
            if (userResult.Success && userResult.Value != null && !string.IsNullOrEmpty(userResult.Value.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userResult.Value.Token);
            }
        }
        catch
        {
            // Si hay error obteniendo el token, continuar sin autorización
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error en GET {endpoint}: Status={response.StatusCode}, Body={errorBody}");
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GET {endpoint}: {ex.Message}");
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            }
            
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en POST {endpoint}: {ex.Message}");
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

    // Métodos con query strings
    public async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string?> queryParams)
    {
        var queryString = BuildQueryString(queryParams);
        var fullEndpoint = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
        return await GetAsync<T>(fullEndpoint);
    }

    private string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var queryParams = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        
        return string.Join("&", queryParams);
    }
}

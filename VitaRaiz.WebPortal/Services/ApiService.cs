using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VitaRaiz.WebPortal.Models;
using NLog;

namespace VitaRaiz.WebPortal.Services;

/// <summary>
/// Thin HTTP client wrapper. Reads the bearer token from the
/// <see cref="CustomAuthenticationStateProvider"/> so every request
/// is automatically authenticated.
/// </summary>
public class ApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly CustomAuthenticationStateProvider _authProvider;
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiService(IHttpClientFactory factory,
                      CustomAuthenticationStateProvider authProvider)
    {
        _factory      = factory;
        _authProvider = authProvider;
    }

    /// <summary>Resolves a server-relative photo path to an absolute URL using the API base address.</summary>
    public string GetPhotoUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
        var client = _factory.CreateClient("VitaRaizApi");
        var baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
        var path    = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";
        return $"{baseUrl}{path}";
    }

    /// <summary>
    /// Fetches a sale photo from the API server-side with the bearer token and
    /// returns a base64 data URL safe for Blazor Server (browser never calls the API directly).
    /// Returns empty string when <paramref name="photoId"/> is null/zero or on any error.
    /// </summary>
    public async Task<string> GetPhotoDataUrlAsync(int? photoId)
    {
        if (photoId is null or 0) return string.Empty;
        try
        {
            var client   = await BuildClientAsync();
            var response = await client.GetAsync($"api/sales/photos/{photoId}/file");
            if (!response.IsSuccessStatusCode) return string.Empty;
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var ct    = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return $"data:{ct};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return string.Empty;
        }
    }

    // -- Helpers -----------------------------------------------------------

    private async Task<HttpClient> BuildClientAsync()
    {
        var client = _factory.CreateClient("VitaRaizApi");
        var user   = await _authProvider.GetCurrentUserAsync();
        if (!string.IsNullOrWhiteSpace(user?.Token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }

    private static string BuildUrl(string endpoint,
        Dictionary<string, string?>? qs = null)
    {
        if (qs is null or { Count: 0 }) return endpoint;
        var query = string.Join("&",
            qs.Where(kv => kv.Value is not null)
              .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
        return string.IsNullOrEmpty(query) ? endpoint : $"{endpoint}?{query}";
    }

    // -- CRUD methods ------------------------------------------------------

    public async Task<T?> GetAsync<T>(string endpoint,
        Dictionary<string, string?>? qs = null)
    {
        try
        {
            var client = await BuildClientAsync();
            var url    = BuildUrl(endpoint, qs);
            var resp   = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _log.Warn("GET {Url} returned {Status}", url, resp.StatusCode);
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<T>(_json);
        }
        catch (Exception ex) { _log.Error(ex, "GET {Url}", endpoint); return default; }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint, TRequest body)
    {
        try
        {
            var client = await BuildClientAsync();
            var resp   = await client.PostAsJsonAsync(endpoint, body, _json);
            if (!resp.IsSuccessStatusCode)
            {
                _log.Warn("POST {Url} returned {Status}", endpoint, resp.StatusCode);
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<TResponse>(_json);
        }
        catch (Exception ex) { _log.Error(ex, "POST {Url}", endpoint); return default; }
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest body)
    {
        try
        {
            var client = await BuildClientAsync();
            var resp   = await client.PostAsJsonAsync(endpoint, body, _json);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) { _log.Error(ex, "POST {Url}", endpoint); return false; }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string endpoint, TRequest body)
    {
        try
        {
            var client = await BuildClientAsync();
            var resp   = await client.PutAsJsonAsync(endpoint, body, _json);
            if (!resp.IsSuccessStatusCode) return default;
            return await resp.Content.ReadFromJsonAsync<TResponse>(_json);
        }
        catch (Exception ex) { _log.Error(ex, "PUT {Url}", endpoint); return default; }
    }

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest body)
    {
        try
        {
            var client = await BuildClientAsync();
            var resp   = await client.PutAsJsonAsync(endpoint, body, _json);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) { _log.Error(ex, "PUT {Url}", endpoint); return false; }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var client = await BuildClientAsync();
            var resp   = await client.DeleteAsync(endpoint);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) { _log.Error(ex, "DELETE {Url}", endpoint); return false; }
    }

    /// <summary>Returns (success, errorMessage). On 422/400 returns the server's message field.</summary>
    public async Task<(bool Ok, string? Error)> DeleteWithMessageAsync(string endpoint)
    {
        try
        {
            var client = await BuildClientAsync();
            var resp   = await client.DeleteAsync(endpoint);
            if (resp.IsSuccessStatusCode) return (true, null);
            string? msg = null;
            try
            {
                var body = await resp.Content.ReadFromJsonAsync<ErrorBody>(_json);
                msg = body?.Message;
            }
            catch { /* ignore parse errors */ }
            return (false, msg ?? $"Error {(int)resp.StatusCode}");
        }
        catch (Exception ex) { _log.Error(ex, "DELETE {Url}", endpoint); return (false, "Error de conexión"); }
    }

    private record ErrorBody(string? Message);

    public async Task<string?> UploadProductPhotoAsync(int productId, Stream fileStream, string fileName)
    {
        try
        {
            var client  = await BuildClientAsync();
            using var content = new MultipartFormDataContent();
            using var sc = new StreamContent(fileStream);
            content.Add(sc, "file", fileName);
            var resp = await client.PostAsync($"api/products/{productId}/photo", content);
            if (!resp.IsSuccessStatusCode) return null;
            var result = await resp.Content.ReadFromJsonAsync<PhotoUploadResult>(_json);
            return result?.PhotoUrl;
        }
        catch (Exception ex) { _log.Error(ex, "UPLOAD product photo {Id}", productId); return null; }
    }

    private record PhotoUploadResult(string PhotoUrl);

    public async Task<int> UploadSalePhotoAsync(int saleId, string photoType, Stream fileStream, string fileName)
    {
        try
        {
            var client = await BuildClientAsync();
            using var content = new MultipartFormDataContent();
            using var sc = new StreamContent(fileStream);
            content.Add(sc, "file", fileName);
            content.Add(new StringContent(photoType), "photoType");
            var resp = await client.PostAsync($"api/sales/{saleId}/photos/upload", content);
            if (!resp.IsSuccessStatusCode) return 0;
            var result = await resp.Content.ReadFromJsonAsync<SalePhotoUploadResult>(_json);
            return result?.PhotoId ?? 0;
        }
        catch (Exception ex) { _log.Error(ex, "UPLOAD sale photo {SaleId} {Type}", saleId, photoType); return 0; }
    }

    private record SalePhotoUploadResult(int PhotoId, string FilePath);

    // -- Unauthenticated (login) -------------------------------------------

    public async Task<TResponse?> PostUnauthAsync<TRequest, TResponse>(
        string endpoint, TRequest body)
    {
        try
        {
            var client = _factory.CreateClient("VitaRaizApi");
            var resp   = await client.PostAsJsonAsync(endpoint, body, _json);
            if (!resp.IsSuccessStatusCode)
            {
                // Try to deserialise error body so callers can surface the message
                try { return await resp.Content.ReadFromJsonAsync<TResponse>(_json); } catch { }
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<TResponse>(_json);
        }
        catch (Exception ex) { _log.Error(ex, "POST-UNAUTH {Url}", endpoint); return default; }
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VitaRaiz.Mobile.Data;

public class SyncService
{
    private readonly LocalDatabase _localDatabase;
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://localhost:7001/api"; // TODO: Cambiar a URL de producción

    public event EventHandler<SyncStatusEventArgs>? SyncStatusChanged;

    public SyncService(LocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(API_BASE_URL),
            Timeout = TimeSpan.FromSeconds(30)
        };
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

    public async Task<SyncResult> SyncAllAsync()
    {
        var result = new SyncResult();

        if (!await IsOnlineAsync())
        {
            result.Success = false;
            result.Message = "No hay conexión a internet";
            OnSyncStatusChanged("Error", result.Message);
            return result;
        }

        try
        {
            // Obtener token de autenticación
            var token = await SecureStorage.GetAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                result.Success = false;
                result.Message = "No hay sesión activa";
                OnSyncStatusChanged("Error", result.Message);
                return result;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            OnSyncStatusChanged("Sincronizando", "Descargando datos del servidor...");

            // 1. Descargar datos del servidor (Pull)
            await SyncCustomersFromServerAsync();
            await SyncSalesFromServerAsync();

            OnSyncStatusChanged("Sincronizando", "Enviando cambios locales...");

            // 2. Enviar cambios locales al servidor (Push)
            var paymentsSync = await SyncPaymentsToServerAsync();
            result.PaymentsSynced = paymentsSync;

            var queueSync = await ProcessSyncQueueAsync();
            result.QueueItemsSynced = queueSync;

            // 3. Limpiar datos sincronizados
            await _localDatabase.ClearSyncedItemsAsync();

            result.Success = true;
            result.Message = $"Sincronización completada. {paymentsSync} pagos y {queueSync} operaciones sincronizadas.";
            OnSyncStatusChanged("Completado", result.Message);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error de sincronización: {ex.Message}";
            OnSyncStatusChanged("Error", result.Message);
            Console.WriteLine($"Sync error: {ex}");
        }

        return result;
    }

    private async Task SyncCustomersFromServerAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/customers");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var customers = JsonSerializer.Deserialize<List<CustomerDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (customers != null)
                {
                    foreach (var customer in customers)
                    {
                        var localCustomer = new LocalCustomer
                        {
                            CustomerId = customer.CustomerId,
                            CustomerName = customer.CustomerName,
                            PhoneNumber = customer.PhoneNumber,
                            Email = customer.Email,
                            Address = customer.Address,
                            ZoneId = customer.ZoneId,
                            GpsLatitude = customer.GpsLatitude,
                            GpsLongitude = customer.GpsLongitude,
                            IsGoldCustomer = customer.IsGoldCustomer,
                            IsBlacklisted = customer.IsBlacklisted,
                            LastSync = DateTime.UtcNow
                        };
                        await _localDatabase.SaveCustomerAsync(localCustomer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing customers: {ex.Message}");
        }
    }

    private async Task SyncSalesFromServerAsync()
    {
        try
        {
            var userId = await SecureStorage.GetAsync("user_id");
            var response = await _httpClient.GetAsync($"/sales/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var sales = JsonSerializer.Deserialize<List<SaleDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sales != null)
                {
                    foreach (var sale in sales)
                    {
                        var localSale = new LocalSale
                        {
                            SaleId = sale.SaleId,
                            CustomerId = sale.CustomerId,
                            SellerId = sale.SellerId,
                            SaleDate = sale.SaleDate,
                            TotalAmount = sale.TotalAmount,
                            PendingAmount = sale.PendingAmount,
                            Status = sale.Status,
                            PaymentTermDays = sale.PaymentTermDays,
                            DueDate = sale.DueDate,
                            Notes = sale.Notes,
                            LastSync = DateTime.UtcNow
                        };
                        await _localDatabase.SaveSaleAsync(localSale);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing sales: {ex.Message}");
        }
    }

    private async Task<int> SyncPaymentsToServerAsync()
    {
        int syncedCount = 0;
        try
        {
            var pendingPayments = await _localDatabase.GetPendingSyncPaymentsAsync();
            
            foreach (var payment in pendingPayments)
            {
                try
                {
                    var paymentRequest = new
                    {
                        saleId = payment.SaleId,
                        amount = payment.Amount,
                        collectedBy = payment.CollectedBy,
                        latitude = decimal.Parse(payment.GpsLatitude ?? "0"),
                        longitude = decimal.Parse(payment.GpsLongitude ?? "0"),
                        notes = payment.Notes,
                        paymentDate = payment.PaymentDate
                    };

                    var jsonContent = JsonSerializer.Serialize(paymentRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync("/payments", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<PaymentResponseDto>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result != null)
                        {
                            await _localDatabase.UpdatePaymentStatusAsync(payment.LocalPaymentId, "Synced", result.PaymentId);
                            
                            // Sincronizar fotos asociadas
                            await SyncPaymentPhotosAsync(payment.LocalPaymentId, result.PaymentId);
                            
                            syncedCount++;
                        }
                    }
                    else
                    {
                        await _localDatabase.UpdatePaymentStatusAsync(payment.LocalPaymentId, "Error");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error syncing payment {payment.LocalPaymentId}: {ex.Message}");
                    await _localDatabase.UpdatePaymentStatusAsync(payment.LocalPaymentId, "Error");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SyncPaymentsToServerAsync: {ex.Message}");
        }

        return syncedCount;
    }

    private async Task SyncPaymentPhotosAsync(int localPaymentId, int paymentId)
    {
        try
        {
            var photos = await _localDatabase.GetPaymentPhotosAsync(localPaymentId);
            
            foreach (var photo in photos.Where(p => !p.IsSynced))
            {
                try
                {
                    // TODO: Implementar carga de fotos usando MultipartFormDataContent
                    // Por ahora solo marcar como sincronizada
                    await _localDatabase.MarkPhotoAsSyncedAsync(photo.LocalPhotoId, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error syncing photo {photo.LocalPhotoId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SyncPaymentPhotosAsync: {ex.Message}");
        }
    }

    private async Task<int> ProcessSyncQueueAsync()
    {
        int syncedCount = 0;
        try
        {
            var queueItems = await _localDatabase.GetPendingSyncItemsAsync();
            
            foreach (var item in queueItems)
            {
                try
                {
                    bool success = false;

                    switch (item.EntityType.ToLower())
                    {
                        case "payment":
                            success = await ProcessPaymentSyncAsync(item);
                            break;
                        case "sale":
                            success = await ProcessSaleSyncAsync(item);
                            break;
                        case "customer":
                            success = await ProcessCustomerSyncAsync(item);
                            break;
                    }

                    if (success)
                    {
                        await _localDatabase.MarkAsSyncedAsync(item.Id);
                        syncedCount++;
                    }
                    else
                    {
                        await _localDatabase.IncrementRetryCountAsync(item.Id, "Sync failed");
                    }
                }
                catch (Exception ex)
                {
                    await _localDatabase.IncrementRetryCountAsync(item.Id, ex.Message);
                    Console.WriteLine($"Error processing sync queue item {item.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessSyncQueueAsync: {ex.Message}");
        }

        return syncedCount;
    }

    private async Task<bool> ProcessPaymentSyncAsync(SyncQueueItem item)
    {
        try
        {
            var content = new StringContent(item.JsonData, Encoding.UTF8, "application/json");
            HttpResponseMessage response;

            switch (item.Operation.ToLower())
            {
                case "create":
                    response = await _httpClient.PostAsync("/payments", content);
                    break;
                case "update":
                    response = await _httpClient.PutAsync("/payments", content);
                    break;
                case "delete":
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    var id = data?["id"]?.ToString();
                    response = await _httpClient.DeleteAsync($"/payments/{id}");
                    break;
                default:
                    return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProcessSaleSyncAsync(SyncQueueItem item)
    {
        try
        {
            var content = new StringContent(item.JsonData, Encoding.UTF8, "application/json");
            HttpResponseMessage response;

            switch (item.Operation.ToLower())
            {
                case "create":
                    response = await _httpClient.PostAsync("/sales", content);
                    break;
                case "update":
                    response = await _httpClient.PutAsync("/sales", content);
                    break;
                case "delete":
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    var id = data?["id"]?.ToString();
                    response = await _httpClient.DeleteAsync($"/sales/{id}");
                    break;
                default:
                    return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProcessCustomerSyncAsync(SyncQueueItem item)
    {
        try
        {
            var content = new StringContent(item.JsonData, Encoding.UTF8, "application/json");
            HttpResponseMessage response;

            switch (item.Operation.ToLower())
            {
                case "create":
                    response = await _httpClient.PostAsync("/customers", content);
                    break;
                case "update":
                    response = await _httpClient.PutAsync("/customers", content);
                    break;
                case "delete":
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    var id = data?["id"]?.ToString();
                    response = await _httpClient.DeleteAsync($"/customers/{id}");
                    break;
                default:
                    return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void OnSyncStatusChanged(string status, string message)
    {
        SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs
        {
            Status = status,
            Message = message,
            Timestamp = DateTime.Now
        });
    }
}

// DTOs
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? ZoneId { get; set; }
    public string? GpsLatitude { get; set; }
    public string? GpsLongitude { get; set; }
    public bool IsGoldCustomer { get; set; }
    public bool IsBlacklisted { get; set; }
}

public class SaleDto
{
    public int SaleId { get; set; }
    public int CustomerId { get; set; }
    public int SellerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class PaymentResponseDto
{
    public int PaymentId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PaymentsSynced { get; set; }
    public int QueueItemsSynced { get; set; }
}

public class SyncStatusEventArgs : EventArgs
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

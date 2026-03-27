using System.Text;
using System.Text.Json;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Data;

public class SyncService
{
    private readonly LocalDatabase _localDatabase;
    private readonly ApiService _apiService;

    public event EventHandler<SyncStatusEventArgs>? SyncStatusChanged;

    public SyncService(LocalDatabase localDatabase, ApiService apiService)
    {
        _localDatabase = localDatabase;
        _apiService = apiService;
    }

    public async Task<bool> IsOnlineAsync()
    {
        return await _apiService.IsOnlineAsync();
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
            // Verificar token
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                result.Success = false;
                result.Message = "No hay sesión activa";
                OnSyncStatusChanged("Error", result.Message);
                return result;
            }

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
            var customers = await _apiService.GetAsync<List<CustomerDto>>("/api/customers");

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
            var sales = await _apiService.GetAsync<List<SaleDto>>("/api/sales/active");

            if (sales != null)
            {
                foreach (var sale in sales)
                {
                    var localSale = new LocalSale
                    {
                        SaleId = sale.SaleId,
                        CustomerId = 0, // TODO: Extraer del DTO si está disponible
                        SellerId = 0,
                        SaleDate = sale.SaleDate,
                        TotalAmount = sale.TotalAmount,
                        PendingAmount = sale.Balance,
                        Status = sale.Status,
                        PaymentTermDays = 30,
                        DueDate = sale.SaleDate.AddDays(30),
                        Notes = sale.PaymentTerms,
                        LastSync = DateTime.UtcNow
                    };
                    await _localDatabase.SaveSaleAsync(localSale);
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
                        collectorId = payment.CollectedBy,
                        gpsLatitude = decimal.TryParse(payment.GpsLatitude, out var lat) ? lat : (decimal?)null,
                        gpsLongitude = decimal.TryParse(payment.GpsLongitude, out var lon) ? lon : (decimal?)null,
                        notes = payment.Notes
                    };

                    var success = await _apiService.PostAsync("/api/payments", paymentRequest);

                    if (success)
                    {
                        await _localDatabase.UpdatePaymentStatusAsync(payment.LocalPaymentId, "Synced", 0);
                        syncedCount++;
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
            switch (item.Operation.ToLower())
            {
                case "create":
                    var createRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    return await _apiService.PostAsync<Dictionary<string, object>>("/api/payments", createRequest!);
                case "update":
                    var updateRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    return await _apiService.PutAsync<Dictionary<string, object>>("/api/payments", updateRequest!);
                case "delete":
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    var id = data?["id"]?.ToString();
                    return await _apiService.DeleteAsync($"/api/payments/{id}");
                default:
                    return false;
            }
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
            switch (item.Operation.ToLower())
            {
                case "create":
                    var createRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    return await _apiService.PostAsync<Dictionary<string, object>>("/api/sales", createRequest!);
                case "update":
                    var updateRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    return await _apiService.PutAsync<Dictionary<string, object>>("/api/sales", updateRequest!);
                case "delete":
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    var id = data?["id"]?.ToString();
                    return await _apiService.DeleteAsync($"/api/sales/{id}");
                default:
                    return false;
            }
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
            switch (item.Operation.ToLower())
            {
                case "create":
                    var createRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    return await _apiService.PostAsync<Dictionary<string, object>>("/api/customers", createRequest!);
                case "update":
                    var updateRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    return await _apiService.PutAsync<Dictionary<string, object>>("/api/customers", updateRequest!);
                case "delete":
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(item.JsonData);
                    var id = data?["id"]?.ToString();
                    return await _apiService.DeleteAsync($"/api/customers/{id}");
                default:
                    return false;
            }
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

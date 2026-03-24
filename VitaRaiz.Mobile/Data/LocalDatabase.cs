using SQLite;

namespace VitaRaiz.Mobile.Data;

public class LocalDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public LocalDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        
        // Crear tablas
        _database.CreateTableAsync<LocalCustomer>().Wait();
        _database.CreateTableAsync<LocalSale>().Wait();
        _database.CreateTableAsync<LocalPayment>().Wait();
        _database.CreateTableAsync<LocalPaymentPhoto>().Wait();
        _database.CreateTableAsync<SyncQueueItem>().Wait();
    }

    #region Customers
    public Task<List<LocalCustomer>> GetCustomersAsync()
    {
        return _database.Table<LocalCustomer>().ToListAsync();
    }

    public Task<LocalCustomer> GetCustomerAsync(int customerId)
    {
        return _database.Table<LocalCustomer>()
            .Where(c => c.CustomerId == customerId)
            .FirstOrDefaultAsync();
    }

    public Task<int> SaveCustomerAsync(LocalCustomer customer)
    {
        return _database.InsertOrReplaceAsync(customer);
    }

    public Task<int> DeleteCustomerAsync(LocalCustomer customer)
    {
        return _database.DeleteAsync(customer);
    }
    #endregion

    #region Sales
    public Task<List<LocalSale>> GetSalesAsync()
    {
        return _database.Table<LocalSale>().ToListAsync();
    }

    public Task<List<LocalSale>> GetActiveSalesAsync()
    {
        return _database.Table<LocalSale>()
            .Where(s => s.Status == "Active")
            .ToListAsync();
    }

    public Task<LocalSale> GetSaleAsync(int saleId)
    {
        return _database.Table<LocalSale>()
            .Where(s => s.SaleId == saleId)
            .FirstOrDefaultAsync();
    }

    public Task<int> SaveSaleAsync(LocalSale sale)
    {
        return _database.InsertOrReplaceAsync(sale);
    }
    #endregion

    #region Payments
    public Task<List<LocalPayment>> GetPaymentsAsync()
    {
        return _database.Table<LocalPayment>().ToListAsync();
    }

    public Task<List<LocalPayment>> GetPendingSyncPaymentsAsync()
    {
        return _database.Table<LocalPayment>()
            .Where(p => p.Status != "Synced")
            .ToListAsync();
    }

    public Task<LocalPayment> GetPaymentAsync(int localPaymentId)
    {
        return _database.Table<LocalPayment>()
            .Where(p => p.LocalPaymentId == localPaymentId)
            .FirstOrDefaultAsync();
    }

    public Task<int> SavePaymentAsync(LocalPayment payment)
    {
        if (payment.LocalPaymentId == 0)
            return _database.InsertAsync(payment);
        else
            return _database.UpdateAsync(payment);
    }

    public Task<int> UpdatePaymentStatusAsync(int localPaymentId, string status, int? paymentId = null)
    {
        var payment = GetPaymentAsync(localPaymentId).Result;
        if (payment != null)
        {
            payment.Status = status;
            payment.LastSync = DateTime.UtcNow;
            if (paymentId.HasValue)
                payment.PaymentId = paymentId.Value;
            return _database.UpdateAsync(payment);
        }
        return Task.FromResult(0);
    }
    #endregion

    #region Payment Photos
    public Task<List<LocalPaymentPhoto>> GetPaymentPhotosAsync(int localPaymentId)
    {
        return _database.Table<LocalPaymentPhoto>()
            .Where(p => p.LocalPaymentId == localPaymentId)
            .ToListAsync();
    }

    public Task<List<LocalPaymentPhoto>> GetUnsyncedPhotosAsync()
    {
        return _database.Table<LocalPaymentPhoto>()
            .Where(p => !p.IsSynced)
            .ToListAsync();
    }

    public Task<int> SavePaymentPhotoAsync(LocalPaymentPhoto photo)
    {
        if (photo.LocalPhotoId == 0)
            return _database.InsertAsync(photo);
        else
            return _database.UpdateAsync(photo);
    }

    public Task<int> MarkPhotoAsSyncedAsync(int localPhotoId, int photoId)
    {
        var photo = _database.Table<LocalPaymentPhoto>()
            .Where(p => p.LocalPhotoId == localPhotoId)
            .FirstOrDefaultAsync().Result;
        
        if (photo != null)
        {
            photo.IsSynced = true;
            photo.PhotoId = photoId;
            return _database.UpdateAsync(photo);
        }
        return Task.FromResult(0);
    }
    #endregion

    #region Sync Queue
    public Task<List<SyncQueueItem>> GetPendingSyncItemsAsync()
    {
        return _database.Table<SyncQueueItem>()
            .Where(s => !s.IsSynced && s.RetryCount < 5)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public Task<int> AddToSyncQueueAsync(SyncQueueItem item)
    {
        return _database.InsertAsync(item);
    }

    public Task<int> MarkAsSyncedAsync(int syncItemId)
    {
        var item = _database.Table<SyncQueueItem>()
            .Where(s => s.Id == syncItemId)
            .FirstOrDefaultAsync().Result;
        
        if (item != null)
        {
            item.IsSynced = true;
            return _database.UpdateAsync(item);
        }
        return Task.FromResult(0);
    }

    public Task<int> IncrementRetryCountAsync(int syncItemId, string errorMessage)
    {
        var item = _database.Table<SyncQueueItem>()
            .Where(s => s.Id == syncItemId)
            .FirstOrDefaultAsync().Result;
        
        if (item != null)
        {
            item.RetryCount++;
            item.ErrorMessage = errorMessage;
            return _database.UpdateAsync(item);
        }
        return Task.FromResult(0);
    }

    public Task<int> ClearSyncedItemsAsync()
    {
        return _database.ExecuteAsync("DELETE FROM SyncQueue WHERE IsSynced = 1");
    }
    #endregion

    #region Statistics
    public async Task<decimal> GetTodayPaymentsTotalAsync(int collectorId)
    {
        var today = DateTime.Today;
        var payments = await _database.Table<LocalPayment>()
            .Where(p => p.CollectedBy == collectorId && p.PaymentDate >= today)
            .ToListAsync();
        
        return payments.Sum(p => p.Amount);
    }

    public async Task<int> GetTodaySalesCountAsync(int sellerId)
    {
        var today = DateTime.Today;
        var sales = await _database.Table<LocalSale>()
            .Where(s => s.SellerId == sellerId && s.SaleDate >= today)
            .ToListAsync();
        
        return sales.Count;
    }

    public async Task<decimal> GetPendingAmountAsync()
    {
        var sales = await _database.Table<LocalSale>()
            .Where(s => s.Status == "Active")
            .ToListAsync();
        
        return sales.Sum(s => s.PendingAmount);
    }
    #endregion
}

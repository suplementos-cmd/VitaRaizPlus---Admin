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
        _database.CreateTableAsync<LocalSalePhoto>().Wait();
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

    #region SalePhotos
    public Task<int> SaveSalePhotoAsync(LocalSalePhoto photo)
    {
        return _database.InsertAsync(photo);
    }

    public Task<List<LocalSalePhoto>> GetSalePhotosAsync(int saleId)
    {
        return _database.Table<LocalSalePhoto>()
            .Where(p => p.SaleId == saleId)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene todas las fotos de múltiples ventas en una sola consulta SQL (evita el problema N+1).
    /// </summary>
    public Task<List<LocalSalePhoto>> GetSalePhotosBatchAsync(IEnumerable<int> saleIds)
    {
        var ids = string.Join(",", saleIds);
        if (string.IsNullOrEmpty(ids))
            return Task.FromResult(new List<LocalSalePhoto>());

        return _database.QueryAsync<LocalSalePhoto>(
            $"SELECT * FROM LocalSalePhotos WHERE SaleId IN ({ids})");
    }

    /// <summary>
    /// Gets the best photo for a sale (priority: Fachada > Cliente > Contrato > Adicional)
    /// </summary>
    public async Task<string?> GetSaleThumbnailAsync(int saleId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LocalDatabase] GetSaleThumbnailAsync START for saleId={saleId}");
            
            var photos = await _database.Table<LocalSalePhoto>()
                .Where(p => p.SaleId == saleId)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"[LocalDatabase] Found {photos?.Count ?? 0} photos for sale {saleId}");

            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocalDatabase]   Photo: Type={photo.PhotoType}, Path={photo.LocalPath}");
                }
            }

            foreach (var type in new[] { "Fachada", "Cliente", "Contrato", "Adicional" })
            {
                var photo = photos?.FirstOrDefault(p => p.PhotoType == type);
                if (photo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocalDatabase] Checking {type}: {photo.LocalPath}");
                    bool exists = File.Exists(photo.LocalPath);
                    System.Diagnostics.Debug.WriteLine($"[LocalDatabase] File.Exists: {exists}");
                    
                    if (exists)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalDatabase] Returning thumbnail: {photo.LocalPath}");
                        return photo.LocalPath;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LocalDatabase] No valid thumbnail found for sale {saleId}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalDatabase] ERROR in GetSaleThumbnailAsync: {ex.Message}");
            return null;
        }
    }
    #endregion
}

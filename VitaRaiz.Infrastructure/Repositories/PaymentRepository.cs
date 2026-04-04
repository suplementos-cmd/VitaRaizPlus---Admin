using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Constants;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class PaymentRepository : BaseOracleRepository, IPaymentRepository
{
    private readonly ICatalogRepository _catalogRepository;

    public PaymentRepository(VitaRaizDbContext context, ICatalogRepository catalogRepository) : base(context)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<int> RegisterPaymentAsync(Payment payment)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_payment");

        var paymentIdParam = AddOutputParameter(command, "p_payment_id");

        AddInputParameter(command, "p_sale_id", payment.SaleId);
        AddInputParameter(command, "p_collector_id", payment.CollectorId);
        AddInputParameter(command, "p_amount", payment.Amount);
        AddInputParameter(command, "p_gps_lat", payment.GpsLatitude);
        AddInputParameter(command, "p_gps_lon", payment.GpsLongitude);
        AddInputParameter(command, "p_device_id", "WEB_API");
        AddInputParameter(command, "p_photo_path", DBNull.Value);
        AddInputParameter(command, "p_notes", payment.Notes);
        AddInputParameter(command, "p_action_id", (object?)payment.CollectionActionId ?? DBNull.Value);
        AddInputParameter(command, "p_sub_id", (object?)payment.CollectionSubId ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)paymentIdParam);
    }

    public async Task<bool> ApprovePaymentAsync(int paymentId, int approvedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_approve_payment");

        AddInputParameter(command, "p_payment_id", paymentId);
        AddInputParameter(command, "p_approved_by", approvedBy);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> RejectPaymentAsync(int paymentId, int rejectedBy, string? reason)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_reject_payment");

        AddInputParameter(command, "p_payment_id", paymentId);
        AddInputParameter(command, "p_rejected_by", rejectedBy);
        AddInputParameter(command, "p_reason", reason);

        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(int? saleId, int? customerId, int? collectorId, 
        DateTime? startDate, DateTime? endDate, string? status)
    {
        try
        {
            Console.WriteLine($"[PaymentRepository] GetPaymentsAsync - Params: saleId={saleId}, customerId={customerId}, collectorId={collectorId}, startDate={startDate}, endDate={endDate}, status={status}");
            
            var query = _context.Payments
                .Include(p => p.Sale)
                    .ThenInclude(s => s.Customer)
                        .ThenInclude(c => c.Zone)
                .Include(p => p.Sale)
                    .ThenInclude(s => s.SalePhotos)
                .Include(p => p.Collector)
                .AsQueryable();

            if (saleId.HasValue)
                query = query.Where(p => p.SaleId == saleId.Value);

            if (customerId.HasValue)
                query = query.Where(p => p.Sale.CustomerId == customerId.Value);

            if (collectorId.HasValue)
                query = query.Where(p => p.CollectorId == collectorId.Value);

            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);

            // Load catalog once — used for both filter and projection
            var statusCatalog = await _catalogRepository.GetPaymentStatusesAsync();
            var statusById  = statusCatalog.ToDictionary(s => s.StatusId,   s => s.StatusCode.ToLower());
            var statusByKey = statusCatalog.ToDictionary(s => s.StatusCode.ToUpper(), s => s.StatusId);

            if (!string.IsNullOrEmpty(status))
            {
                // Resolve status ID from catalog (case-insensitive match on StatusCode)
                if (statusByKey.TryGetValue(status.ToUpper(), out var sid))
                    query = query.Where(p => p.StatusId == sid);
            }

            var rawPayments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var payments = rawPayments.Select(p => new PaymentDto
            {
                PaymentId     = p.PaymentId,
                SaleId        = p.SaleId,
                CollectorId   = p.CollectorId,
                CustomerName  = p.Sale.Customer.CustomerName,
                Amount        = p.Amount,
                PaymentDate   = p.PaymentDate,
                CollectorName = p.Collector.Username,
                Status        = statusById.TryGetValue(p.StatusId, out var code) ? code : p.StatusId.ToString(),
                Notes         = p.Notes,
                GpsLatitude   = p.GpsLatitude,
                GpsLongitude  = p.GpsLongitude,
                ZoneName      = p.Sale.Customer.Zone?.ZoneName,
                SaleStatus    = p.Sale.Status,
                SaleBalance   = p.Sale.TotalAmount - p.Sale.PaidAmount,
                CustomerPhotoUrl = p.Sale.SalePhotos
                    .Where(ph => ph.PhotoType == "CLIENTE")
                    .OrderByDescending(ph => ph.UploadedAt)
                    .Select(ph => ph.ThumbnailPath ?? ph.FilePath)
                    .FirstOrDefault(),
                FacadePhotoUrl = p.Sale.SalePhotos
                    .Where(ph => ph.PhotoType == "FACHADA")
                    .OrderByDescending(ph => ph.UploadedAt)
                    .Select(ph => ph.ThumbnailPath ?? ph.FilePath)
                    .FirstOrDefault(),
            }).ToList();

            Console.WriteLine($"[PaymentRepository] Devolviendo {payments.Count} pagos");
            return payments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentRepository] ERROR: {ex.Message}");
            throw;
        }
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(int paymentId)
    {
        try
        {
            Console.WriteLine($"[PaymentRepository] GetPaymentByIdAsync - paymentId={paymentId}");

            var statusCatalog = await _catalogRepository.GetPaymentStatusesAsync();
            var statusById = statusCatalog.ToDictionary(s => s.StatusId, s => s.StatusCode.ToLower());

            var raw = await _context.Payments
                .Include(p => p.Sale)
                    .ThenInclude(s => s.Customer)
                        .ThenInclude(c => c.Zone)
                .Include(p => p.Sale)
                    .ThenInclude(s => s.SalePhotos)
                .Include(p => p.Collector)
                .Where(p => p.PaymentId == paymentId)
                .FirstOrDefaultAsync();

            if (raw == null) return null;

            Console.WriteLine($"[PaymentRepository] Pago encontrado: {raw.PaymentId}");
            return new PaymentDto
            {
                PaymentId     = raw.PaymentId,
                SaleId        = raw.SaleId,
                CollectorId   = raw.CollectorId,
                CustomerName  = raw.Sale.Customer.CustomerName,
                Amount        = raw.Amount,
                PaymentDate   = raw.PaymentDate,
                CollectorName = raw.Collector.Username,
                Status        = statusById.TryGetValue(raw.StatusId, out var code) ? code : raw.StatusId.ToString(),
                Notes         = raw.Notes,
                GpsLatitude   = raw.GpsLatitude,
                GpsLongitude  = raw.GpsLongitude,
                ZoneName      = raw.Sale.Customer.Zone?.ZoneName,
                SaleStatus    = raw.Sale.Status,
                SaleBalance   = raw.Sale.TotalAmount - raw.Sale.PaidAmount,
                CustomerPhotoUrl = raw.Sale.SalePhotos
                    .Where(ph => ph.PhotoType == "CLIENTE")
                    .OrderByDescending(ph => ph.UploadedAt)
                    .Select(ph => ph.ThumbnailPath ?? ph.FilePath)
                    .FirstOrDefault(),
                FacadePhotoUrl = raw.Sale.SalePhotos
                    .Where(ph => ph.PhotoType == "FACHADA")
                    .OrderByDescending(ph => ph.UploadedAt)
                    .Select(ph => ph.ThumbnailPath ?? ph.FilePath)
                    .FirstOrDefault(),
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentRepository] ERROR en GetPaymentByIdAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<Payment>> GetPaymentsBySaleIdAsync(int saleId)
    {
        return await _context.Payments
            .Where(p => p.SaleId == saleId)
            .Include(p => p.Collector)
            .Include(p => p.PaymentPhotos)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.Sale)
            .Include(p => p.Collector)
            .Include(p => p.PaymentPhotos)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }

    public async Task<bool> UpdatePaymentAsync(int paymentId, decimal amount, DateTime paymentDate, 
        string status, string? notes)
    {
        try
        {
            Console.WriteLine($"[PaymentRepository] UpdatePaymentAsync - paymentId={paymentId}, amount={amount}, status={status}");
            
            var payment = await _context.Payments.FindAsync(paymentId);
            
            if (payment == null)
            {
                Console.WriteLine($"[PaymentRepository] Payment {paymentId} not found");
                return false;
            }

            // Update properties
            payment.Amount = amount;
            payment.PaymentDate = paymentDate;

            // Resolve status ID dynamically from catalog (STATUS_KEY match, case-insensitive)
            var statuses = await _catalogRepository.GetPaymentStatusesAsync();
            var matched = statuses.FirstOrDefault(s =>
                string.Equals(s.StatusCode, status, StringComparison.OrdinalIgnoreCase));
            payment.StatusId = matched?.StatusId ?? PaymentStatusCodes.Pending; // fallback: PENDING

            payment.Notes = notes;

            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[PaymentRepository] Payment {paymentId} updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentRepository] ERROR in UpdatePaymentAsync: {ex.Message}");
            throw;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class PaymentRepository : BaseOracleRepository, IPaymentRepository
{
    public PaymentRepository(VitaRaizDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Normaliza el status de pago de Oracle a lowercase consistente.
    /// Oracle usa: PENDING, APPROVED, REJECTED
    /// API devuelve: pending, approved, rejected
    /// </summary>
    private static string NormalizePaymentStatus(string? status)
        => status?.ToLower() ?? "unknown";

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

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status.ToLower() == status.ToLower());

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    SaleId = p.SaleId,
                    CustomerName = p.Sale.Customer.CustomerName,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    CollectorName = p.Collector.Username,
                    Status = p.Status, // Se normaliza después de materializar
                    Notes = p.Notes,
                    GpsLatitude = p.GpsLatitude,
                    GpsLongitude = p.GpsLongitude
                })
                .ToListAsync();

            // Normalizar status después de materializar la query
            foreach (var payment in payments)
                payment.Status = NormalizePaymentStatus(payment.Status);

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
            
            var payment = await _context.Payments
                .Include(p => p.Sale)
                    .ThenInclude(s => s.Customer)
                .Include(p => p.Collector)
                .Where(p => p.PaymentId == paymentId)
                .Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    SaleId = p.SaleId,
                    CustomerName = p.Sale.Customer.CustomerName,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    CollectorName = p.Collector.Username,
                    Status = p.Status, // Se normaliza abajo
                    Notes = p.Notes,
                    GpsLatitude = p.GpsLatitude,
                    GpsLongitude = p.GpsLongitude
                })
                .FirstOrDefaultAsync();

            if (payment != null)
                payment.Status = NormalizePaymentStatus(payment.Status);

            Console.WriteLine($"[PaymentRepository] Pago encontrado: {payment != null}");
            return payment;
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
}

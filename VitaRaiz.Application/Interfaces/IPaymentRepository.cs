using VitaRaiz.Application.DTOs;
using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface IPaymentRepository
{
    Task<int> RegisterPaymentAsync(Payment payment);
    
    Task<bool> ApprovePaymentAsync(int paymentId, int approvedBy);
    
    Task<bool> RejectPaymentAsync(int paymentId, int rejectedBy, string? reason);
    
    Task<List<PaymentDto>> GetPaymentsAsync(int? saleId, int? customerId, int? collectorId, 
        DateTime? startDate, DateTime? endDate, string? status);
    
    Task<PaymentDto?> GetPaymentByIdAsync(int paymentId);
    
    Task<IEnumerable<Payment>> GetPaymentsBySaleIdAsync(int saleId);
    
    Task<Payment?> GetByIdAsync(int paymentId);
}

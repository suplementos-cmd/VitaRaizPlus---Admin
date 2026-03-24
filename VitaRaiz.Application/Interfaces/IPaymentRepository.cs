using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface IPaymentRepository
{
    Task<int> RegisterPaymentAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPaymentsBySaleIdAsync(int saleId);
    Task<Payment?> GetByIdAsync(int paymentId);
}

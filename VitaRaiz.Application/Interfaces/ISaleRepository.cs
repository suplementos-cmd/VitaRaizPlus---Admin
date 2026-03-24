using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface ISaleRepository
{
    Task<int> CreateSaleAsync(Sale sale);
    Task<Sale?> GetByIdAsync(int saleId);
    Task<decimal> GetSaleBalanceAsync(int saleId);
    Task<string> GetSaleRiskStatusAsync(int saleId);
}

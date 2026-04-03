using VitaRaiz.Application.DTOs;
using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface ISaleRepository
{
    Task<int> CreateSaleAsync(int customerId, int sellerId, int paymentTermDays, 
        string? paymentTerm, string? collectionDay, DateTime? firstCollectionDate, decimal downPayment,
        string? notes, List<SaleDetailDto> details);
    
    Task<bool> UpdateSaleAsync(int saleId, string? paymentTerm, string? collectionDay,
        DateTime? firstCollectionDate, decimal downPayment, string? notes, string? status,
        DateTime saleDate);
    
    Task<bool> CancelSaleAsync(int saleId, string? reason);
    
    Task<List<SaleDto>> GetSalesAsync(int? customerId, int? sellerId, 
        DateTime? startDate, DateTime? endDate, string? status);
    
    Task<List<SaleDto>> GetActiveSalesAsync(int? collectorId);
    
    Task<SaleDto?> GetSaleByIdAsync(int saleId);
    
    Task<Sale?> GetByIdAsync(int saleId);
    
    Task<decimal> GetSaleBalanceAsync(int saleId);
    
    Task<string> GetSaleRiskStatusAsync(int saleId);
    
    Task<SaleFullDto?> GetSaleFullAsync(int saleId);
}

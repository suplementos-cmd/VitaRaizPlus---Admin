using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Commands.Sales;

public class CreateSaleCommand : IRequest<int>
{
    public int CustomerId { get; set; }
    public int SellerId { get; set; }
    public int PaymentTermDays { get; set; }
    
    // Campos estructurados de pago
    public string? PaymentTerm { get; set; } = "SEMANAL"; // SEMANAL, QUINCENAL, MENSUAL, CONTADO
    public string? CollectionDay { get; set; } // LUN, MAR, MIE, JUE, VIE, SAB, DOM
    public DateTime? FirstCollectionDate { get; set; } // Fecha del primer cobro programado
    public decimal DownPayment { get; set; } = 0; // Enganche/pago inicial
    
    public string? Notes { get; set; }
    public List<SaleDetailDto> Details { get; set; } = new();
}

public class UpdateSaleCommand : IRequest<bool>
{
    public int SaleId { get; set; }
    public string? PaymentTerm { get; set; }
    public string? CollectionDay { get; set; }
    public DateTime? FirstCollectionDate { get; set; }
    public decimal DownPayment { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public DateTime SaleDate { get; set; }
    // Customer fields (updated separately via CustomerRepository)
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int? ZoneId { get; set; }
}

public class CancelSaleCommand : IRequest<bool>
{
    public int SaleId { get; set; }
    public string? Reason { get; set; }
}

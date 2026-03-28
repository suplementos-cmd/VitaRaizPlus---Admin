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

public class CancelSaleCommand : IRequest<bool>
{
    public int SaleId { get; set; }
    public string? Reason { get; set; }
}

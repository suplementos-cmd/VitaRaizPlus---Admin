using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Commands.Sales;

public class CreateSaleCommand : IRequest<int>
{
    public int CustomerId { get; set; }
    public int SellerId { get; set; }
    public int PaymentTermDays { get; set; }
    public string? Notes { get; set; }
    public List<SaleDetailDto> Details { get; set; } = new();
}

public class CancelSaleCommand : IRequest<bool>
{
    public int SaleId { get; set; }
    public string? Reason { get; set; }
}

using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Queries.Sales;

public class GetSalesQuery : IRequest<List<SaleDto>>
{
    public int? CustomerId { get; set; }
    public int? SellerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}

public class GetActiveSalesQuery : IRequest<List<SaleDto>>
{
    public int? CollectorId { get; set; }
}

public class GetSaleByIdQuery : IRequest<SaleDto?>
{
    public int SaleId { get; set; }
}

public class GetSaleFullQuery : IRequest<SaleFullDto?>
{
    public int SaleId { get; set; }
}

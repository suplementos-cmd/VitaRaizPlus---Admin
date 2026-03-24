using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Queries.Payments;

public class GetPaymentsQuery : IRequest<List<PaymentDto>>
{
    public int? SaleId { get; set; }
    public int? CustomerId { get; set; }
    public int? CollectorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}

public class GetPaymentByIdQuery : IRequest<PaymentDto?>
{
    public int PaymentId { get; set; }
}

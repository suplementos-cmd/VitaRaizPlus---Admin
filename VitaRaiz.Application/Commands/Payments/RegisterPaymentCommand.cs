using MediatR;

namespace VitaRaiz.Application.Commands.Payments;

public class RegisterPaymentCommand : IRequest<int>
{
    public int SaleId { get; set; }
    public int CollectorId { get; set; }
    public decimal Amount { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string? Notes { get; set; }
    public int? CollectionActionId { get; set; }
    public int? CollectionSubId { get; set; }
}

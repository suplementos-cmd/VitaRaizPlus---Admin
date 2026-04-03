using MediatR;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Commands.Payments;

public class RegisterPaymentCommandHandler : IRequestHandler<RegisterPaymentCommand, int>
{
    private readonly IPaymentRepository _paymentRepository;

    public RegisterPaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<int> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = new Payment
        {
            SaleId = request.SaleId,
            CollectorId = request.CollectorId,
            Amount = request.Amount,
            GpsLatitude = request.GpsLatitude,
            GpsLongitude = request.GpsLongitude,
            Notes = request.Notes,
            CollectionActionId = request.CollectionActionId,
            CollectionSubId = request.CollectionSubId,
            PaymentDate = DateTime.Now,
            StatusId = 1
        };

        var paymentId = await _paymentRepository.RegisterPaymentAsync(payment);
        return paymentId;
    }
}

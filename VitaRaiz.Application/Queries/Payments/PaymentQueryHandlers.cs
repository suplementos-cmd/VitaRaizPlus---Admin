using MediatR;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Queries.Payments;

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, List<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<List<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        return await _paymentRepository.GetPaymentsAsync(
            request.SaleId,
            request.CustomerId,
            request.CollectorId,
            request.StartDate,
            request.EndDate,
            request.Status
        );
    }
}

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _paymentRepository.GetPaymentByIdAsync(request.PaymentId);
    }
}

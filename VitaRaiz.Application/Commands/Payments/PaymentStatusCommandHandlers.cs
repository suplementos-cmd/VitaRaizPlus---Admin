using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Commands.Payments;

public class ApprovePaymentCommandHandler : IRequestHandler<ApprovePaymentCommand, bool>
{
    private readonly IPaymentRepository _paymentRepository;

    public ApprovePaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<bool> Handle(ApprovePaymentCommand request, CancellationToken cancellationToken)
    {
        return await _paymentRepository.ApprovePaymentAsync(request.PaymentId, request.ApprovedBy);
    }
}

public class RejectPaymentCommandHandler : IRequestHandler<RejectPaymentCommand, bool>
{
    private readonly IPaymentRepository _paymentRepository;

    public RejectPaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<bool> Handle(RejectPaymentCommand request, CancellationToken cancellationToken)
    {
        return await _paymentRepository.RejectPaymentAsync(request.PaymentId, request.RejectedBy, request.Reason);
    }
}

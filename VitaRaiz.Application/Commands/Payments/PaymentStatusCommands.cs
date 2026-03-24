using MediatR;

namespace VitaRaiz.Application.Commands.Payments;

public class ApprovePaymentCommand : IRequest<bool>
{
    public int PaymentId { get; set; }
    public int ApprovedBy { get; set; }
}

public class RejectPaymentCommand : IRequest<bool>
{
    public int PaymentId { get; set; }
    public int RejectedBy { get; set; }
    public string? Reason { get; set; }
}

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

public class UpdatePaymentCommand : IRequest<bool>
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}


using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Commands.Sales;

public class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, int>
{
    private readonly ISaleRepository _saleRepository;

    public CreateSaleCommandHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<int> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        return await _saleRepository.CreateSaleAsync(
            request.CustomerId,
            request.SellerId,
            request.PaymentTermDays,
            request.PaymentTerm,
            request.CollectionDay,
            request.FirstCollectionDate,
            request.DownPayment,
            request.Notes,
            request.Details
        );
    }
}

public class CancelSaleCommandHandler : IRequestHandler<CancelSaleCommand, bool>
{
    private readonly ISaleRepository _saleRepository;

    public CancelSaleCommandHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<bool> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        return await _saleRepository.CancelSaleAsync(request.SaleId, request.Reason);
    }
}

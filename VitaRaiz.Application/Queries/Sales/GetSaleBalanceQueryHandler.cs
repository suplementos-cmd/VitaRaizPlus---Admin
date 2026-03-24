using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Queries.Sales;

public class GetSaleBalanceQueryHandler : IRequestHandler<GetSaleBalanceQuery, decimal>
{
    private readonly ISaleRepository _saleRepository;

    public GetSaleBalanceQueryHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<decimal> Handle(GetSaleBalanceQuery request, CancellationToken cancellationToken)
    {
        var balance = await _saleRepository.GetSaleBalanceAsync(request.SaleId);
        return balance;
    }
}

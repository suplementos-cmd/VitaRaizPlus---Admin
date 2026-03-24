using MediatR;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Queries.Sales;

public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, List<SaleDto>>
{
    private readonly ISaleRepository _saleRepository;

    public GetSalesQueryHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<List<SaleDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        return await _saleRepository.GetSalesAsync(
            request.CustomerId,
            request.SellerId,
            request.StartDate,
            request.EndDate,
            request.Status
        );
    }
}

public class GetActiveSalesQueryHandler : IRequestHandler<GetActiveSalesQuery, List<SaleDto>>
{
    private readonly ISaleRepository _saleRepository;

    public GetActiveSalesQueryHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<List<SaleDto>> Handle(GetActiveSalesQuery request, CancellationToken cancellationToken)
    {
        return await _saleRepository.GetActiveSalesAsync(request.CollectorId);
    }
}

public class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, SaleDto?>
{
    private readonly ISaleRepository _saleRepository;

    public GetSaleByIdQueryHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<SaleDto?> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        return await _saleRepository.GetSaleByIdAsync(request.SaleId);
    }
}

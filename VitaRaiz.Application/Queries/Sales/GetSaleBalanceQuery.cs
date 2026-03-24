using MediatR;

namespace VitaRaiz.Application.Queries.Sales;

public class GetSaleBalanceQuery : IRequest<decimal>
{
    public int SaleId { get; set; }

    public GetSaleBalanceQuery(int saleId)
    {
        SaleId = saleId;
    }
}

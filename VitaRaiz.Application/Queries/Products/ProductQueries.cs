using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Queries.Products;

public class GetProductsQuery : IRequest<List<ProductDto>>
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}

public class GetProductByIdQuery : IRequest<ProductDto?>
{
    public int ProductId { get; set; }
}

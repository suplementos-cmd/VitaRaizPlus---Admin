using MediatR;

namespace VitaRaiz.Application.Commands.Products;

public class CreateProductCommand : IRequest<int>
{
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int? CategoryId { get; set; }
}

public class UpdateProductCommand : IRequest<bool>
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public int? CategoryId { get; set; }
}

public class DeleteProductCommand : IRequest<bool>
{
    public int ProductId { get; set; }
}

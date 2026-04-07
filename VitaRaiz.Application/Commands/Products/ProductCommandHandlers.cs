using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Commands.Products;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return await _productRepository.CreateProductAsync(
            request.ProductName,
            request.Description,
            request.UnitPrice,
            request.Stock,
            request.Category
        );
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return await _productRepository.UpdateProductAsync(
            request.ProductId,
            request.ProductName,
            request.Description,
            request.UnitPrice,
            request.Stock,
            request.IsActive,
            request.Category
        );
    }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        return await _productRepository.DeleteProductAsync(request.ProductId);
    }
}

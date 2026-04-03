using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Commands.Sales;

public class UpdateSaleCommandHandler : IRequestHandler<UpdateSaleCommand, bool>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;

    public UpdateSaleCommandHandler(ISaleRepository saleRepository, ICustomerRepository customerRepository)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
    }

    public async Task<bool> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var saleUpdated = await _saleRepository.UpdateSaleAsync(
            request.SaleId,
            request.PaymentTerm,
            request.CollectionDay,
            request.FirstCollectionDate,
            request.DownPayment,
            request.Notes,
            request.Status,
            request.SaleDate
        );

        if (!saleUpdated) return false;

        // Update customer contact fields, preserving email/GPS/flags we don't expose in the form
        if (request.CustomerId > 0 && !string.IsNullOrEmpty(request.CustomerName))
        {
            var existing = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
            if (existing != null)
            {
                await _customerRepository.UpdateCustomerAsync(
                    request.CustomerId,
                    request.CustomerName,
                    request.PhoneNumber,
                    existing.Email,
                    request.Address,
                    request.ZoneId,
                    existing.GpsLatitude?.ToString("F6"),
                    existing.GpsLongitude?.ToString("F6"),
                    existing.IsGoldCustomer,
                    existing.IsBlacklisted
                );
            }
        }

        return true;
    }
}

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

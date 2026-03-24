using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Commands.Customers;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, int>
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<int> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        return await _customerRepository.CreateCustomerAsync(
            request.CustomerName,
            request.PhoneNumber,
            request.Email,
            request.Address,
            request.ZoneId,
            request.GpsLatitude,
            request.GpsLongitude,
            request.IsGoldCustomer,
            request.IsBlacklisted
        );
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;

    public UpdateCustomerCommandHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<bool> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        return await _customerRepository.UpdateCustomerAsync(
            request.CustomerId,
            request.CustomerName,
            request.PhoneNumber,
            request.Email,
            request.Address,
            request.ZoneId,
            request.GpsLatitude,
            request.GpsLongitude,
            request.IsGoldCustomer,
            request.IsBlacklisted
        );
    }
}

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;

    public DeleteCustomerCommandHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        return await _customerRepository.DeleteCustomerAsync(request.CustomerId);
    }
}

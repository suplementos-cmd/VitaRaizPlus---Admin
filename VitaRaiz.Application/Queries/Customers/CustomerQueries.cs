using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Queries.Customers;

public class GetCustomersQuery : IRequest<List<CustomerDto>>
{
    public string? SearchTerm { get; set; }
    public int? ZoneId { get; set; }
    public bool? IsGoldCustomer { get; set; }
    public bool? IsBlacklisted { get; set; }
}

public class GetCustomerByIdQuery : IRequest<CustomerDto?>
{
    public int CustomerId { get; set; }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;
using VitaRaiz.Infrastructure.Repositories;

namespace VitaRaiz.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Registrar DbContext con Oracle
        services.AddDbContext<VitaRaizDbContext>(options =>
            options.UseOracle(configuration.GetConnectionString("OracleConnection")));

        // Registrar repositorios
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ISalePhotoRepository, SalePhotoRepository>();
        services.AddScoped<IPermissionsRepository, PermissionsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }
}

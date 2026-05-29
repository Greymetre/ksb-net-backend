using Application.Interfaces.Services;
using Application.Mappings;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(_ => { }, typeof(LaravelMappingProfile).Assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILoyaltySchemeService, LoyaltySchemeService>();
        services.AddScoped<INewInvoiceService, NewInvoiceService>();
        services.AddScoped<IRedemptionService, RedemptionService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}

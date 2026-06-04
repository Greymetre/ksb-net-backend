using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Build connection string from environment variables or appsettings
        var connectionString = BuildConnectionString(configuration);

        var serverVersion = configuration["Database:MySqlVersion"] ?? "8.0.36";
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(Version.Parse(serverVersion))));

        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IMasterDataRepository, MasterDataRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoyaltySchemeRepository, LoyaltySchemeRepository>();
        services.AddScoped<INewInvoiceRepository, NewInvoiceRepository>();
        services.AddScoped<IRedemptionRepository, RedemptionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHrRepository, HrRepository>();
        services.AddScoped<ICityAssignmentRepository, CityAssignmentRepository>();
        services.AddScoped<IUserTargetRepository, UserTargetRepository>();
        services.AddScoped<IExpenseTypeRepository, ExpenseTypeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        // Try to get from environment variables first (Railway sets these)
        var host = Environment.GetEnvironmentVariable("MYSQLHOST") ?? configuration["MYSQLHOST"];
        var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? configuration["MYSQLPORT"];
        var database = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? configuration["MYSQLDATABASE"];
        var user = Environment.GetEnvironmentVariable("MYSQLUSER") ?? configuration["MYSQLUSER"];
        var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? configuration["MYSQLPASSWORD"];

        // Use defaults if not set
        host ??= "127.0.0.1";
        port ??= "3306";
        database ??= "netksb_new";
        user ??= "root";
        password ??= "";

        return $"Server={host};Port={port};Database={database};User={user};Password={password};";
    }
}


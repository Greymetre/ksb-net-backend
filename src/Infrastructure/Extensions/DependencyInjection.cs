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
        var connectionString = ResolveConnectionString(configuration);

        var serverVersion = configuration["Database:MySqlVersion"] ?? "8.0.36";
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(Version.Parse(serverVersion)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

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
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var directConnectionString = Environment.GetEnvironmentVariable("KSB_PR_CONNECTION");
        if (!string.IsNullOrWhiteSpace(directConnectionString))
        {
            return directConnectionString;
        }

        var railwayUrl = Environment.GetEnvironmentVariable("MYSQL_URL")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL");

        if (!string.IsNullOrWhiteSpace(railwayUrl))
        {
            return BuildConnectionStringFromUrl(railwayUrl);
        }

        var railwayHost = Environment.GetEnvironmentVariable("MYSQLHOST");
        var railwayDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE");
        var railwayUser = Environment.GetEnvironmentVariable("MYSQLUSER");
        var railwayPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD");

        if (!string.IsNullOrWhiteSpace(railwayHost)
            && !string.IsNullOrWhiteSpace(railwayDatabase)
            && !string.IsNullOrWhiteSpace(railwayUser))
        {
            var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
            return BuildMySqlConnectionString(railwayHost, port, railwayDatabase, railwayUser, railwayPassword ?? string.Empty);
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    }

    private static string BuildConnectionStringFromUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        return BuildMySqlConnectionString(
            uri.Host,
            uri.Port > 0 ? uri.Port.ToString() : "3306",
            Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
            userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty);
    }

    private static string BuildMySqlConnectionString(string server, string port, string database, string user, string password)
    {
        return string.Join(';',
            $"Server={EscapeConnectionStringValue(server)}",
            $"Port={EscapeConnectionStringValue(port)}",
            $"Database={EscapeConnectionStringValue(database)}",
            $"User={EscapeConnectionStringValue(user)}",
            $"Password={EscapeConnectionStringValue(password)}",
            "SslMode=Preferred");
    }

    private static string EscapeConnectionStringValue(string value)
    {
        return value.Replace(";", "\\;", StringComparison.Ordinal);
    }
}

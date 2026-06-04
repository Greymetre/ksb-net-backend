using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Api.Extensions;
using Api.Middleware;
using Application.Extensions;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Infrastructure.Seeders;
using Infrastructure.Seeders.MasterData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLaravelCompatibleSwagger();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var allowedCorsOrigins = GetCorsOrigins(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(tokenId))
                {
                    context.Fail("Token id missing.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var revoked = await dbContext.OAuthAccessTokens.AnyAsync(x => x.Id == tokenId && x.Revoked, context.HttpContext.RequestAborted);
                if (revoked)
                {
                    context.Fail("Token revoked.");
                }
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        if (allowedCorsOrigins.Length == 0)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedCorsOrigins);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (HasFlag(args, "--migrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Database migration completed.");
    return;
}

if (HasFlag(args, "--seed-superadmin"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SuperAdminSeeder.SeedAsync(dbContext);
    Console.WriteLine("Seeder completed: superadmin role and gajendra user are ready.");
    return;
}

if (HasFlag(args, "--seed-master-data"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await MasterDataSeeder.SeedAsync(dbContext);
    Console.WriteLine("Seeder completed: master data tables are ready.");
    return;
}

app.UseMiddleware<LaravelExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("FrontendCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

static bool HasFlag(string[] args, string flag)
{
    return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
}

static string[] GetCorsOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration["CORS"]
        ?? configuration["Cors"]
        ?? configuration["Cors:AllowedOrigins"]
        ?? configuration["CORS_ALLOWED_ORIGINS"];

    return (configuredOrigins ?? string.Empty)
        .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(origin => origin.TrimEnd('/'))
        .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

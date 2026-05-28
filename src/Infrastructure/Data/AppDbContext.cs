using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<NewInvoice> NewInvoices => Set<NewInvoice>();
    public DbSet<NewInvoiceApprovalLog> NewInvoiceApprovalLogs => Set<NewInvoiceApprovalLog>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<ModelHasRole> ModelHasRoles => Set<ModelHasRole>();
    public DbSet<ModelHasPermission> ModelHasPermissions => Set<ModelHasPermission>();
    public DbSet<RoleHasPermission> RoleHasPermissions => Set<RoleHasPermission>();
    public DbSet<MobileUserLoginDetail> MobileUserLoginDetails => Set<MobileUserLoginDetail>();
    public DbSet<OAuthAccessToken> OAuthAccessTokens => Set<OAuthAccessToken>();
    public DbSet<UserDetails> UserDetails => Set<UserDetails>();
    public DbSet<UserCityAssign> UserCityAssigns => Set<UserCityAssign>();
    public DbSet<UserEducation> UserEducation => Set<UserEducation>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Pincode> Pincodes => Set<Pincode>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductFamily> ProductFamilies => Set<ProductFamily>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();
    public DbSet<LoyaltyScheme> LoyaltySchemes => Set<LoyaltyScheme>();
    public DbSet<LoyaltySchemeSlab> LoyaltySchemeSlabs => Set<LoyaltySchemeSlab>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

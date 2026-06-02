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
    public DbSet<LoyaltyRedemption> LoyaltyRedemptions => Set<LoyaltyRedemption>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<CompOffLeave> CompOffLeaves => Set<CompOffLeave>();
    public DbSet<TourProgramme> TourProgrammes => Set<TourProgramme>();
    public DbSet<TourDetail> TourDetails => Set<TourDetail>();
    public DbSet<TourLog> TourLogs => Set<TourLog>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Beat> Beats => Set<Beat>();
    public DbSet<BeatSchedule> BeatSchedules => Set<BeatSchedule>();
    public DbSet<SalesTargetUser> SalesTargetUsers => Set<SalesTargetUser>();
    public DbSet<PrimarySale> PrimarySales => Set<PrimarySale>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseLog> ExpenseLogs => Set<ExpenseLog>();
    public DbSet<Media> Media => Set<Media>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

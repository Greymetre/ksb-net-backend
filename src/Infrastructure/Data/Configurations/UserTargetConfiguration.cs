using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class SalesTargetUserConfiguration : IEntityTypeConfiguration<SalesTargetUser>
{
    public void Configure(EntityTypeBuilder<SalesTargetUser> builder)
    {
        builder.ToTable("salestargetusers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.BranchId).HasColumnName("branch_id");
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(255);
        builder.Property(x => x.Month).HasColumnName("month").HasMaxLength(255);
        builder.Property(x => x.Year).HasColumnName("year").HasColumnType("year");
        builder.Property(x => x.Target).HasColumnName("target").HasPrecision(10, 2);
        builder.Property(x => x.Achievement).HasColumnName("achievement").HasPrecision(10, 2);
        builder.Property(x => x.AchievementPercent).HasColumnName("achievement_percent").HasPrecision(10, 2);
        builder.Property(x => x.QuantityTarget).HasColumnName("qunatity_target").HasPrecision(10, 2);
        builder.Property(x => x.QuantityAchievement).HasColumnName("qunatity_achievement").HasPrecision(10, 2);
        builder.Property(x => x.QuantityAchievementPercent).HasColumnName("qunatity_achievement_percent").HasPrecision(10, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class PrimarySaleConfiguration : IEntityTypeConfiguration<PrimarySale>
{
    public void Configure(EntityTypeBuilder<PrimarySale> builder)
    {
        builder.ToTable("primary_sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InvoiceDate).HasColumnName("invoice_date");
        builder.Property(x => x.BranchId).HasColumnName("branch_id");
        builder.Property(x => x.EmpCode).HasColumnName("emp_code").HasMaxLength(255);
        builder.Property(x => x.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrderDate).HasColumnName("order_date");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.SubTotal).HasColumnName("sub_total").HasPrecision(19, 2);
        builder.Property(x => x.TotalQty).HasColumnName("total_qty").HasPrecision(19, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => x.CreatedBy);
    }
}

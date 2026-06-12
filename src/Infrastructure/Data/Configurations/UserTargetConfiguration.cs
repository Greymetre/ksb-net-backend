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
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.BuyerId).HasColumnName("buyer_id");
        builder.Property(x => x.SellerId).HasColumnName("seller_id");
        builder.Property(x => x.ExecutiveId).HasColumnName("executive_id");
        builder.Property(x => x.TotalQty).HasColumnName("total_qty");
        builder.Property(x => x.ShippedQty).HasColumnName("shipped_qty");
        builder.Property(x => x.OrderNo).HasColumnName("orderno").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.OrderDate).HasColumnName("order_date");
        builder.Property(x => x.CompletedDate).HasColumnName("completed_date");
        builder.Property(x => x.EstimatedDate).HasColumnName("estimated_date");
        builder.Property(x => x.TotalGst).HasColumnName("total_gst").HasPrecision(19, 2);
        builder.Property(x => x.TotalDiscount).HasColumnName("total_discount").HasPrecision(19, 2);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(19, 2);
        builder.Property(x => x.ExtraDiscount).HasColumnName("extra_discount").HasPrecision(8, 2);
        builder.Property(x => x.ExtraDiscountAmount).HasColumnName("extra_discount_amount").HasPrecision(19, 2);
        builder.Property(x => x.SubTotal).HasColumnName("sub_total").HasPrecision(19, 2);
        builder.Property(x => x.GrandTotal).HasColumnName("grand_total").HasPrecision(19, 2);
        builder.Property(x => x.OrderTaking).HasColumnName("order_taking").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.AddressId).HasColumnName("address_id");
        builder.Property(x => x.SucDel).HasColumnName("suc_del").HasMaxLength(191);
        builder.Property(x => x.GstAmount).HasColumnName("gst_amount").HasMaxLength(125);
        builder.Property(x => x.SchmeAmount).HasColumnName("schme_amount").HasPrecision(19, 2);
        builder.Property(x => x.SchmeVal).HasColumnName("schme_val").HasPrecision(19, 2);
        builder.Property(x => x.EbdAmount).HasColumnName("ebd_amount").HasPrecision(19, 2);
        builder.Property(x => x.EbdDiscount).HasColumnName("ebd_discount").HasPrecision(19, 2);
        builder.Property(x => x.SpecialDiscount).HasColumnName("special_discount");
        builder.Property(x => x.SpecialAmount).HasColumnName("special_amount").HasPrecision(19, 2);
        builder.Property(x => x.ClusterDiscount).HasColumnName("cluster_discount");
        builder.Property(x => x.ClusterAmount).HasColumnName("cluster_amount").HasPrecision(19, 2);
        builder.Property(x => x.DealDiscount).HasColumnName("deal_discount");
        builder.Property(x => x.DealAmount).HasColumnName("deal_amount").HasPrecision(19, 2);
        builder.Property(x => x.DistributorDiscount).HasColumnName("distributor_discount");
        builder.Property(x => x.DistributorAmount).HasColumnName("distributor_amount").HasPrecision(19, 2);
        builder.Property(x => x.FrieghtDiscount).HasColumnName("frieght_discount");
        builder.Property(x => x.FrieghtAmount).HasColumnName("frieght_amount").HasPrecision(19, 2);
        builder.Property(x => x.ProductCatId).HasColumnName("product_cat_id");
        builder.Property(x => x.DodDiscount).HasColumnName("dod_discount").HasPrecision(10, 2);
        builder.Property(x => x.CashDiscount).HasColumnName("cash_discount").HasPrecision(10, 2);
        builder.Property(x => x.CashAmount).HasColumnName("cash_amount").HasPrecision(10, 2);
        builder.Property(x => x.AgriStandardDiscount).HasColumnName("agri_standard_discount").HasPrecision(10, 2);
        builder.Property(x => x.AgriStandardDiscountAmount).HasColumnName("agri_standard_discount_amount").HasPrecision(10, 2);
        builder.Property(x => x.Advance).HasColumnName("advance").HasPrecision(19, 2);
        builder.Property(x => x.Gst5Amt).HasColumnName("gst5_amt").HasPrecision(10, 2);
        builder.Property(x => x.Gst12Amt).HasColumnName("gst12_amt").HasPrecision(10, 2);
        builder.Property(x => x.Gst18Amt).HasColumnName("gst18_amt").HasPrecision(10, 2);
        builder.Property(x => x.Gst28Amt).HasColumnName("gst28_amt").HasPrecision(10, 2);
        builder.Property(x => x.OrderRemark).HasColumnName("order_remark").HasMaxLength(255);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.BeatScheduleId).HasColumnName("beatscheduleid");
        builder.Property(x => x.OrderType).HasColumnName("order_type").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => x.CreatedBy);
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

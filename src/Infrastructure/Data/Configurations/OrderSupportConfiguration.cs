using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("order_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.ProductDetailId).HasColumnName("product_detail_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.ShippedQty).HasColumnName("shipped_qty");
        builder.Property(x => x.Price).HasColumnName("price").HasPrecision(19, 2);
        builder.Property(x => x.Discount).HasColumnName("discount").HasPrecision(19, 2);
        builder.Property(x => x.Gst).HasColumnName("gst").HasPrecision(19, 2);
        builder.Property(x => x.GstAmount).HasColumnName("gst_amount").HasPrecision(19, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(19, 2);
        builder.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasPrecision(19, 2);
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(19, 2);
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.SchemeName).HasColumnName("scheme_name").HasMaxLength(255);
        builder.Property(x => x.SchemeDiscount).HasColumnName("scheme_discount").HasPrecision(10, 2);
        builder.Property(x => x.SchemeAmount).HasColumnName("scheme_amount").HasPrecision(10, 2);
        builder.Property(x => x.ClusterDiscount).HasColumnName("cluster_discount").HasPrecision(10, 2);
        builder.Property(x => x.ClusterAmount).HasColumnName("cluster_amount").HasPrecision(10, 2);
        builder.Property(x => x.DealDiscount).HasColumnName("deal_discount").HasPrecision(10, 2);
        builder.Property(x => x.DealAmount).HasColumnName("deal_amount").HasPrecision(10, 2);
        builder.Property(x => x.DistributorDiscount).HasColumnName("distributor_discount").HasPrecision(10, 2);
        builder.Property(x => x.DistributorAmount).HasColumnName("distributor_amount").HasPrecision(10, 2);
        builder.Property(x => x.FrieghtDiscount).HasColumnName("frieght_discount").HasPrecision(10, 2);
        builder.Property(x => x.FrieghtAmount).HasColumnName("frieght_amount").HasPrecision(10, 2);
        builder.Property(x => x.AgriStandardDis).HasColumnName("agri_standard_dis").HasPrecision(10, 2);
        builder.Property(x => x.AgriStandardDisAmounts).HasColumnName("agri_standard_dis_amounts").HasPrecision(10, 2);
        builder.Property(x => x.EbdDis).HasColumnName("ebd_dis");
        builder.Property(x => x.SpecialDis).HasColumnName("special_dis");
        builder.Property(x => x.SpecialAmounts).HasColumnName("special_amounts").HasPrecision(10, 2);
        builder.Property(x => x.EbdAmount).HasColumnName("ebd_amount").HasPrecision(10, 2);
        builder.Property(x => x.SubcategoryId).HasColumnName("subcategory_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

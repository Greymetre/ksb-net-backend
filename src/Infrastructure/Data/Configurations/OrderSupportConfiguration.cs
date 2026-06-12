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

public sealed class MasterDistributorConfiguration : IEntityTypeConfiguration<MasterDistributor>
{
    public void Configure(EntityTypeBuilder<MasterDistributor> builder)
    {
        builder.ToTable("master_distributors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(255);
        builder.Property(x => x.TradeName).HasColumnName("trade_name").HasMaxLength(255);
        builder.Property(x => x.DistributorCode).HasColumnName("distributor_code").HasMaxLength(255);
        builder.Property(x => x.Mobile).HasColumnName("mobile").HasMaxLength(255);
        builder.Property(x => x.BillingAddress).HasColumnName("billing_address").HasMaxLength(255);
        builder.Property(x => x.BillingCity).HasColumnName("billing_city").HasMaxLength(255);
        builder.Property(x => x.BillingDistrict).HasColumnName("billing_district").HasMaxLength(255);
        builder.Property(x => x.BillingState).HasColumnName("billing_state").HasMaxLength(255);
        builder.Property(x => x.BillingCountry).HasColumnName("billing_country").HasMaxLength(255);
        builder.Property(x => x.BillingPincode).HasColumnName("billing_pincode").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class SecondaryCustomerConfiguration : IEntityTypeConfiguration<SecondaryCustomer>
{
    public void Configure(EntityTypeBuilder<SecondaryCustomer> builder)
    {
        builder.ToTable("secondary_customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(255);
        builder.Property(x => x.OwnerName).HasColumnName("owner_name").HasMaxLength(255);
        builder.Property(x => x.ShopName).HasColumnName("shop_name").HasMaxLength(255);
        builder.Property(x => x.MobileNumber).HasColumnName("mobile_number").HasMaxLength(255);
        builder.Property(x => x.AddressLine).HasColumnName("address_line");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1);
        builder.Property(x => x.CountryId).HasColumnName("country_id");
        builder.Property(x => x.StateId).HasColumnName("state_id");
        builder.Property(x => x.DistrictId).HasColumnName("district_id");
        builder.Property(x => x.CityId).HasColumnName("city_id");
        builder.Property(x => x.PincodeId).HasColumnName("pincode_id");
        builder.Property(x => x.DistributorName).HasColumnName("distributor_name").HasMaxLength(255);
        builder.Property(x => x.AgriDistributor).HasColumnName("agri_distributor").HasMaxLength(255);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

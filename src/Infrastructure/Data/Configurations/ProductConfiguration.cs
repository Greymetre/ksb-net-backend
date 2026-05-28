using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.Ranking).HasColumnName("ranking").HasDefaultValue(1);
        builder.Property(x => x.CategoryName).HasColumnName("category_name").HasMaxLength(250);
        builder.Property(x => x.CategoryImage).HasColumnName("category_image").HasMaxLength(350);
        builder.Property(x => x.SapCode).HasColumnName("sap_code").HasMaxLength(350);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class ProductFamilyConfiguration : IEntityTypeConfiguration<ProductFamily>
{
    public void Configure(EntityTypeBuilder<ProductFamily> builder)
    {
        builder.ToTable("subcategories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.Ranking).HasColumnName("ranking").HasDefaultValue(1);
        builder.Property(x => x.SubcategoryName).HasColumnName("subcategory_name").HasMaxLength(250);
        builder.Property(x => x.SubcategoryImage).HasColumnName("subcategory_image").HasMaxLength(350);
        builder.Property(x => x.SapCode).HasColumnName("sap_code").HasMaxLength(350);
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.ServiceCategoryId).HasColumnName("service_category_id").HasMaxLength(191);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.Ranking).HasColumnName("ranking").HasDefaultValue(1);
        builder.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(250);
        builder.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(125);
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(250);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(450);
        builder.Property(x => x.SubcategoryId).HasColumnName("subcategory_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.ProductImage).HasColumnName("product_image").HasMaxLength(300);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Specification).HasColumnName("specification").HasMaxLength(255);
        builder.Property(x => x.PartNo).HasColumnName("part_no").HasMaxLength(250);
        builder.Property(x => x.ProductNo).HasColumnName("product_no").HasMaxLength(250);
        builder.Property(x => x.ModelNo).HasColumnName("model_no").HasMaxLength(250);
        builder.Property(x => x.SapCode).HasColumnName("sap_code").HasMaxLength(225);
        builder.Property(x => x.HsnSac).HasColumnName("hsn_sac");
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        builder.ToTable("product_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.DetailTitle).HasColumnName("detail_title").HasMaxLength(200);
        builder.Property(x => x.DetailDescription).HasColumnName("detail_description").HasMaxLength(450);
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.DetailImage).HasColumnName("detail_image").HasMaxLength(400);
        builder.Property(x => x.Mrp).HasColumnName("mrp").HasPrecision(8, 2);
        builder.Property(x => x.Price).HasColumnName("price").HasPrecision(8, 2);
        builder.Property(x => x.SellingPrice).HasColumnName("selling_price").HasPrecision(8, 2);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}


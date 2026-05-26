using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.Mobile).HasColumnName("mobile").HasMaxLength(15);
        builder.Property(x => x.ContactNumber).HasColumnName("contact_number").HasMaxLength(15);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(250);
        builder.Property(x => x.Password).HasColumnName("password").HasMaxLength(255).HasDefaultValue("");
        builder.Property(x => x.NotificationId).HasColumnName("notification_id").HasMaxLength(450).HasDefaultValue("");
        builder.Property(x => x.Latitude).HasColumnName("latitude").HasMaxLength(50);
        builder.Property(x => x.Longitude).HasColumnName("longitude").HasMaxLength(50);
        builder.Property(x => x.DeviceType).HasColumnName("device_type").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(20).HasDefaultValue("");
        builder.Property(x => x.ProfileImage).HasColumnName("profile_image").HasMaxLength(350).HasDefaultValue("");
        builder.Property(x => x.ShopImage).HasColumnName("shop_image").HasMaxLength(350);
        builder.Property(x => x.CustomerCode).HasColumnName("customer_code").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.CustomerType).HasColumnName("customertype");
        builder.Property(x => x.RegionId).HasColumnName("region_id");
        builder.Property(x => x.FirmType).HasColumnName("firmtype");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.ExecutiveId).HasColumnName("executive_id");
        builder.Property(x => x.BeatScheduleId).HasColumnName("beatscheduleid");
        builder.Property(x => x.ManagerName).HasColumnName("manager_name").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.ManagerPhone).HasColumnName("manager_phone").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.Otp).HasColumnName("otp").HasMaxLength(20);
        builder.Property(x => x.CustomFields).HasColumnName("custom_fields").HasColumnType("longtext");
        builder.Property(x => x.SameAddress).HasColumnName("same_address");
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.SapCode).HasColumnName("sap_code").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Mobile).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

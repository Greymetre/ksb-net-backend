using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class LoyaltySchemeConfiguration : IEntityTypeConfiguration<LoyaltyScheme>
{
    public void Configure(EntityTypeBuilder<LoyaltyScheme> builder)
    {
        builder.ToTable("loyalty_schemes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.SchemeName).HasColumnName("scheme_name").HasMaxLength(250);
        builder.Property(x => x.SchemeCode).HasColumnName("scheme_code").HasMaxLength(100);
        builder.Property(x => x.SchemeDescription).HasColumnName("scheme_description").HasColumnType("text");
        builder.Property(x => x.SchemeTag).HasColumnName("scheme_tag").HasMaxLength(50);
        builder.Property(x => x.CustomerType).HasColumnName("customer_type").HasMaxLength(100);
        builder.Property(x => x.AreaScope).HasColumnName("area_scope").HasMaxLength(50);
        builder.Property(x => x.AreaValues).HasColumnName("area_values").HasColumnType("json");
        builder.Property(x => x.StartDate).HasColumnName("start_date").HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnName("end_date").HasColumnType("date");
        builder.Property(x => x.SchemeType).HasColumnName("scheme_type").HasMaxLength(50).HasDefaultValue("Invoice");
        builder.Property(x => x.BasedOn).HasColumnName("based_on").HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(x => x.SchemeCode).IsUnique();
        builder.HasIndex(x => x.SchemeName);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Slabs).WithOne(x => x.LoyaltyScheme).HasForeignKey(x => x.LoyaltySchemeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LoyaltySchemeSlabConfiguration : IEntityTypeConfiguration<LoyaltySchemeSlab>
{
    public void Configure(EntityTypeBuilder<LoyaltySchemeSlab> builder)
    {
        builder.ToTable("loyalty_scheme_slabs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LoyaltySchemeId).HasColumnName("loyalty_scheme_id");
        builder.Property(x => x.TierName).HasColumnName("tier_name").HasMaxLength(150);
        builder.Property(x => x.ValueFrom).HasColumnName("value_from").HasPrecision(15, 2);
        builder.Property(x => x.ValueTo).HasColumnName("value_to").HasPrecision(15, 2);
        builder.Property(x => x.RewardValue).HasColumnName("reward_value").HasPrecision(15, 2);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);

        builder.HasIndex(x => x.LoyaltySchemeId);
    }
}

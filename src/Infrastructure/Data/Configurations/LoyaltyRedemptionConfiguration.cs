using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class LoyaltyRedemptionConfiguration : IEntityTypeConfiguration<LoyaltyRedemption>
{
    public void Configure(EntityTypeBuilder<LoyaltyRedemption> builder)
    {
        builder.ToTable("loyalty_redemptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TransactionNo).HasColumnName("transaction_no").HasMaxLength(80);
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.LoyaltySchemeId).HasColumnName("loyalty_scheme_id");
        builder.Property(x => x.WalletType).HasColumnName("wallet_type").HasMaxLength(30);
        builder.Property(x => x.SchemeName).HasColumnName("scheme_name").HasMaxLength(250);
        builder.Property(x => x.RedeemMode).HasColumnName("redeem_mode").HasMaxLength(20);
        builder.Property(x => x.Points).HasColumnName("points").HasPrecision(15, 2);
        builder.Property(x => x.AccountHolder).HasColumnName("account_holder").HasMaxLength(255);
        builder.Property(x => x.AccountNumber).HasColumnName("account_number").HasMaxLength(255);
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.IfscCode).HasColumnName("ifsc_code").HasMaxLength(50);
        builder.Property(x => x.BankConfirmed).HasColumnName("bank_confirmed");
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue(LoyaltyRedemption.StatusPending);
        builder.Property(x => x.Remark).HasColumnName("remark").HasColumnType("text");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.RejectedBy).HasColumnName("rejected_by");
        builder.Property(x => x.RejectedAt).HasColumnName("rejected_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(x => x.TransactionNo).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.LoyaltySchemeId);
        builder.HasIndex(x => x.WalletType);
        builder.HasIndex(x => x.Status);
    }
}

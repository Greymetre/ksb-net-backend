using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class NewInvoiceConfiguration : IEntityTypeConfiguration<NewInvoice>
{
    public void Configure(EntityTypeBuilder<NewInvoice> builder)
    {
        builder.ToTable("new_invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SecondaryCustomerId).HasColumnName("secondary_customer_id");
        builder.Property(x => x.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(255);
        builder.Property(x => x.InvoiceDate).HasColumnName("invoice_date").HasColumnType("date");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(15, 2);
        builder.Property(x => x.Points).HasColumnName("points").HasPrecision(15, 2).HasDefaultValue(0);
        builder.Property(x => x.Attachment).HasColumnName("attachment").HasMaxLength(500);
        builder.Property(x => x.ApprovalStatus).HasColumnName("approval_status").HasDefaultValue(0);
        builder.Property(x => x.ApprovalRemark).HasColumnName("approval_remark").HasColumnType("text");
        builder.Property(x => x.ApprovedSsBy).HasColumnName("approved_ss_by");
        builder.Property(x => x.ApprovedSsAt).HasColumnName("approved_ss_at");
        builder.Property(x => x.ApprovedSalesBy).HasColumnName("approved_sales_by");
        builder.Property(x => x.ApprovedSalesAt).HasColumnName("approved_sales_at");
        builder.Property(x => x.ApprovedHoBy).HasColumnName("approved_ho_by");
        builder.Property(x => x.ApprovedHoAt).HasColumnName("approved_ho_at");
        builder.Property(x => x.RejectedBy).HasColumnName("rejected_by");
        builder.Property(x => x.RejectedAt).HasColumnName("rejected_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.SecondaryCustomerId);
        builder.HasIndex(x => x.CreatedBy);
        builder.HasIndex(x => x.ApprovalStatus);
    }
}

public sealed class NewInvoiceApprovalLogConfiguration : IEntityTypeConfiguration<NewInvoiceApprovalLog>
{
    public void Configure(EntityTypeBuilder<NewInvoiceApprovalLog> builder)
    {
        builder.ToTable("new_invoice_approval_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LogDate).HasColumnName("log_date").HasColumnType("date");
        builder.Property(x => x.NewInvoiceId).HasColumnName("new_invoice_id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.StatusType).HasColumnName("status_type").HasMaxLength(255);
        builder.Property(x => x.FromStatus).HasColumnName("from_status");
        builder.Property(x => x.ToStatus).HasColumnName("to_status");
        builder.Property(x => x.ApprovedAmount).HasColumnName("approved_amount").HasPrecision(15, 2);
        builder.Property(x => x.Remark).HasColumnName("remark").HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);

        builder.HasIndex(x => x.NewInvoiceId);
        builder.HasIndex(x => x.CreatedBy);
    }
}

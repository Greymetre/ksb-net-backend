using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class ExpenseTypeConfiguration : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder)
    {
        builder.ToTable("expenses_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Rate).HasColumnName("rate").HasPrecision(8, 2);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.AllowanceTypeId).HasColumnName("allowance_type_id");
        builder.Property(x => x.PayrollId).HasColumnName("payroll_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ExpensesType).HasColumnName("expenses_type");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Date).HasColumnName("date").HasMaxLength(255);
        builder.Property(x => x.ClaimAmount).HasColumnName("claim_amount").HasPrecision(8, 2);
        builder.Property(x => x.ApproveAmount).HasColumnName("approve_amount").HasPrecision(10, 2);
        builder.Property(x => x.StartKm).HasColumnName("start_km").HasMaxLength(255);
        builder.Property(x => x.StopKm).HasColumnName("stop_km").HasMaxLength(255);
        builder.Property(x => x.TotalKm).HasColumnName("total_km").HasMaxLength(255);
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.CheckerStatus).HasColumnName("checker_status");
        builder.Property(x => x.AccountantStatus).HasColumnName("accountant_status");
        builder.Property(x => x.ApproveRejectBy).HasColumnName("approve_reject_by");
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class ExpenseLogConfiguration : IEntityTypeConfiguration<ExpenseLog>
{
    public void Configure(EntityTypeBuilder<ExpenseLog> builder)
    {
        builder.ToTable("expense_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LogDate).HasColumnName("log_date");
        builder.Property(x => x.ExpenseId).HasColumnName("expense_id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.StatusType).HasColumnName("status_type").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("media");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ModelType).HasColumnName("model_type").HasMaxLength(255);
        builder.Property(x => x.ModelId).HasColumnName("model_id");
        builder.Property(x => x.Uuid).HasColumnName("uuid").HasMaxLength(36);
        builder.Property(x => x.CollectionName).HasColumnName("collection_name").HasMaxLength(255);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255);
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(255);
        builder.Property(x => x.Disk).HasColumnName("disk").HasMaxLength(255);
        builder.Property(x => x.ConversionsDisk).HasColumnName("conversions_disk").HasMaxLength(255);
        builder.Property(x => x.Size).HasColumnName("size");
        builder.Property(x => x.Manipulations).HasColumnName("manipulations");
        builder.Property(x => x.CustomProperties).HasColumnName("custom_properties");
        builder.Property(x => x.GeneratedConversions).HasColumnName("generated_conversions");
        builder.Property(x => x.ResponsiveImages).HasColumnName("responsive_images");
        builder.Property(x => x.OrderColumn).HasColumnName("order_column");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260526054000_NewInvoicesUseCustomers")]
    public partial class NewInvoicesUseCustomers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.REFERENTIAL_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND TABLE_NAME = 'new_invoices'
      AND CONSTRAINT_NAME = 'new_invoices_secondary_customer_id_foreign'
)");
            migrationBuilder.Sql(@"SET @drop_fk_sql := IF(@fk_exists > 0,
    'ALTER TABLE `new_invoices` DROP FOREIGN KEY `new_invoices_secondary_customer_id_foreign`',
    'SELECT 1'
)");
            migrationBuilder.Sql(@"PREPARE drop_fk_stmt FROM @drop_fk_sql");
            migrationBuilder.Sql(@"EXECUTE drop_fk_stmt");
            migrationBuilder.Sql(@"DEALLOCATE PREPARE drop_fk_stmt");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  ADD CONSTRAINT `new_invoices_secondary_customer_id_foreign`
  FOREIGN KEY (`secondary_customer_id`) REFERENCES `customers` (`id`) ON DELETE CASCADE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.REFERENTIAL_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND TABLE_NAME = 'new_invoices'
      AND CONSTRAINT_NAME = 'new_invoices_secondary_customer_id_foreign'
)");
            migrationBuilder.Sql(@"SET @drop_fk_sql := IF(@fk_exists > 0,
    'ALTER TABLE `new_invoices` DROP FOREIGN KEY `new_invoices_secondary_customer_id_foreign`',
    'SELECT 1'
)");
            migrationBuilder.Sql(@"PREPARE drop_fk_stmt FROM @drop_fk_sql");
            migrationBuilder.Sql(@"EXECUTE drop_fk_stmt");
            migrationBuilder.Sql(@"DEALLOCATE PREPARE drop_fk_stmt");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  ADD CONSTRAINT `new_invoices_secondary_customer_id_foreign`
  FOREIGN KEY (`secondary_customer_id`) REFERENCES `secondary_customers` (`id`) ON DELETE CASCADE");
        }
    }
}

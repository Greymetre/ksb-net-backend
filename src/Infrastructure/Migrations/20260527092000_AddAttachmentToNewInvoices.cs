using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260527092000_AddAttachmentToNewInvoices")]
public partial class AddAttachmentToNewInvoices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  ADD COLUMN IF NOT EXISTS `attachment` varchar(500) DEFAULT NULL AFTER `points`;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  DROP COLUMN IF EXISTS `attachment`;");
    }
}

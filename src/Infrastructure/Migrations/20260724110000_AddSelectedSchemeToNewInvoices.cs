using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724110000_AddSelectedSchemeToNewInvoices")]
public sealed class AddSelectedSchemeToNewInvoices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<ulong>("loyalty_scheme_id", "new_invoices", "bigint unsigned", nullable: true);
        migrationBuilder.CreateIndex("IX_new_invoices_loyalty_scheme_id", "new_invoices", "loyalty_scheme_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_new_invoices_loyalty_scheme_id", "new_invoices");
        migrationBuilder.DropColumn("loyalty_scheme_id", "new_invoices");
    }
}

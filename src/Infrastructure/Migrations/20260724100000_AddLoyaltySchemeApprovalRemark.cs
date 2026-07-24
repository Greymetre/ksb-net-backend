using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724100000_AddLoyaltySchemeApprovalRemark")]
public sealed class AddLoyaltySchemeApprovalRemark : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "approval_remark",
            table: "loyalty_schemes",
            type: "varchar(1000)",
            maxLength: 1000,
            nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "approval_remark", table: "loyalty_schemes");
}

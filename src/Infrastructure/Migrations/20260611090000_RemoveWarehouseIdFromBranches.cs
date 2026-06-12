using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260611090000_RemoveWarehouseIdFromBranches")]
public partial class RemoveWarehouseIdFromBranches : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "warehouse_id",
            table: "branches");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "warehouse_id",
            table: "branches",
            type: "varchar(125)",
            maxLength: 125,
            nullable: true);
    }
}

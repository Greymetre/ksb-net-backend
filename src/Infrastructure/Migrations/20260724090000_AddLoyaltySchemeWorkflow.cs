using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724090000_AddLoyaltySchemeWorkflow")]
public sealed class AddLoyaltySchemeWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("brochure_path", "loyalty_schemes", "varchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTime>("submitted_at", "loyalty_schemes", "datetime(6)", nullable: true);
        migrationBuilder.AddColumn<ulong>("submitted_by", "loyalty_schemes", "bigint unsigned", nullable: true);
        migrationBuilder.AddColumn<DateTime>("approved_at", "loyalty_schemes", "datetime(6)", nullable: true);
        migrationBuilder.AddColumn<ulong>("approved_by", "loyalty_schemes", "bigint unsigned", nullable: true);
        migrationBuilder.AddColumn<DateTime>("rejected_at", "loyalty_schemes", "datetime(6)", nullable: true);
        migrationBuilder.AddColumn<ulong>("rejected_by", "loyalty_schemes", "bigint unsigned", nullable: true);
        migrationBuilder.AddColumn<string>("rejection_remark", "loyalty_schemes", "varchar(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("brochure_path", "loyalty_schemes");
        migrationBuilder.DropColumn("submitted_at", "loyalty_schemes");
        migrationBuilder.DropColumn("submitted_by", "loyalty_schemes");
        migrationBuilder.DropColumn("approved_at", "loyalty_schemes");
        migrationBuilder.DropColumn("approved_by", "loyalty_schemes");
        migrationBuilder.DropColumn("rejected_at", "loyalty_schemes");
        migrationBuilder.DropColumn("rejected_by", "loyalty_schemes");
        migrationBuilder.DropColumn("rejection_remark", "loyalty_schemes");
    }
}

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724130000_AddSoftDeleteToLoyaltySchemeSlabs")]
public partial class AddSoftDeleteToLoyaltySchemeSlabs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE `loyalty_scheme_slabs`
ADD COLUMN IF NOT EXISTS `deleted_at` datetime(6) NULL;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE `loyalty_scheme_slabs`
DROP COLUMN IF EXISTS `deleted_at`;
""");
    }
}

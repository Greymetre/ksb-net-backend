using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260608090000_AddInfluencersCustomerType")]
public partial class AddInfluencersCustomerType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
INSERT INTO `customer_types` (`id`, `active`, `customertype_name`, `type_name`, `created_at`, `updated_at`)
VALUES (3, 'Y', 'Influencers', 'Influencers', UTC_TIMESTAMP(), UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
  `active` = VALUES(`active`),
  `customertype_name` = VALUES(`customertype_name`),
  `type_name` = VALUES(`type_name`),
  `updated_at` = UTC_TIMESTAMP();
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM `customer_types` WHERE `id` = 3 AND `customertype_name` = 'Influencers';");
    }
}

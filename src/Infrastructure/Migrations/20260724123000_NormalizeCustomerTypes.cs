using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724123000_NormalizeCustomerTypes")]
public partial class NormalizeCustomerTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
INSERT INTO `customer_types` (`id`, `active`, `customertype_name`, `type_name`, `created_at`, `updated_at`)
VALUES
  (1, 'Y', 'Dealer', 'Dealer', UTC_TIMESTAMP(), UTC_TIMESTAMP()),
  (2, 'Y', 'Retailer', 'Retailer', UTC_TIMESTAMP(), UTC_TIMESTAMP()),
  (3, 'Y', 'Influencer', 'Influencer', UTC_TIMESTAMP(), UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
  `active` = 'Y',
  `customertype_name` = VALUES(`customertype_name`),
  `type_name` = VALUES(`type_name`),
  `updated_at` = UTC_TIMESTAMP();

UPDATE `loyalty_schemes`
SET `customer_type` = CASE
  WHEN LOWER(TRIM(`customer_type`)) IN ('dealer', 'distributor', 'sub-dealer', 'sub dealer') THEN 'Dealer'
  WHEN LOWER(TRIM(`customer_type`)) IN ('influencer', 'influencers', 'plumber') THEN 'Influencer'
  WHEN LOWER(TRIM(`customer_type`)) IN ('retailer', 'retailer + plumber') THEN 'Retailer'
  ELSE `customer_type`
END,
`updated_at` = UTC_TIMESTAMP()
WHERE `customer_type` IS NOT NULL;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
UPDATE `customer_types`
SET `customertype_name` = CASE `id`
  WHEN 1 THEN 'Distributor'
  WHEN 2 THEN 'Retailer'
  WHEN 3 THEN 'Influencers'
  ELSE `customertype_name`
END,
`type_name` = CASE `id`
  WHEN 1 THEN 'Distributor'
  WHEN 2 THEN 'Retailer'
  WHEN 3 THEN 'Influencers'
  ELSE `type_name`
END,
`updated_at` = UTC_TIMESTAMP()
WHERE `id` IN (1, 2, 3);
""");
    }
}

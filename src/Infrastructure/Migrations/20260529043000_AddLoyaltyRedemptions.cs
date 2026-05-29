using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260529043000_AddLoyaltyRedemptions")]
public partial class AddLoyaltyRedemptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `loyalty_redemptions` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `transaction_no` varchar(80) NOT NULL,
  `customer_id` bigint unsigned NOT NULL,
  `loyalty_scheme_id` bigint unsigned DEFAULT NULL,
  `wallet_type` varchar(30) NOT NULL DEFAULT 'Regular',
  `scheme_name` varchar(250) NOT NULL DEFAULT '',
  `redeem_mode` varchar(20) NOT NULL DEFAULT 'NEFT',
  `points` decimal(15,2) NOT NULL DEFAULT 0.00,
  `account_holder` varchar(255) NOT NULL DEFAULT '',
  `account_number` varchar(255) NOT NULL DEFAULT '',
  `bank_name` varchar(255) NOT NULL DEFAULT '',
  `ifsc_code` varchar(50) NOT NULL DEFAULT '',
  `bank_confirmed` tinyint(1) NOT NULL DEFAULT 0,
  `status` int NOT NULL DEFAULT 0,
  `remark` text DEFAULT NULL,
  `created_by` bigint unsigned DEFAULT NULL,
  `approved_by` bigint unsigned DEFAULT NULL,
  `approved_at` timestamp NULL DEFAULT NULL,
  `rejected_by` bigint unsigned DEFAULT NULL,
  `rejected_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `loyalty_redemptions_transaction_no_unique` (`transaction_no`),
  KEY `loyalty_redemptions_customer_id_index` (`customer_id`),
  KEY `loyalty_redemptions_loyalty_scheme_id_index` (`loyalty_scheme_id`),
  KEY `loyalty_redemptions_wallet_type_index` (`wallet_type`),
  KEY `loyalty_redemptions_status_index` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS `loyalty_redemptions`;");
    }
}

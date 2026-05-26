using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260522083000_CompleteMysqlSchemaRelations")]
    public partial class CompleteMysqlSchemaRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET FOREIGN_KEY_CHECKS=0;");
            migrationBuilder.Sql(@"ALTER TABLE `users` ADD COLUMN IF NOT EXISTS `login_at` timestamp NULL DEFAULT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE `customers` ADD COLUMN IF NOT EXISTS `working_status` varchar(255) DEFAULT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE `customers` ADD COLUMN IF NOT EXISTS `creation_date` varchar(255) DEFAULT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE `user_details` ADD COLUMN IF NOT EXISTS `pan_card_image` varchar(225) DEFAULT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE `user_details` ADD COLUMN IF NOT EXISTS `aadhar_card_image` varchar(225) DEFAULT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE `mobile_user_login_details` ADD COLUMN IF NOT EXISTS `active` varchar(1) NOT NULL DEFAULT 'Y';");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `active_customer_processes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) UNSIGNED NOT NULL,
  `process_id` bigint(20) UNSIGNED NOT NULL,
  `assigned_by` bigint(20) UNSIGNED DEFAULT NULL,
  `status` enum('pending','in_progress','completed','cancelled') NOT NULL DEFAULT 'pending',
  `started_at` timestamp NULL DEFAULT NULL,
  `completed_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `active_customer_process_steps` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active_customer_process_id` bigint(20) UNSIGNED NOT NULL,
  `customer_process_step_id` bigint(20) UNSIGNED NOT NULL,
  `status` enum('pending','active','completed') NOT NULL DEFAULT 'pending',
  `completed_by` bigint(20) UNSIGNED DEFAULT NULL,
  `completed_at` timestamp NULL DEFAULT NULL,
  `remark` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `addresses` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `address1` varchar(250) NOT NULL DEFAULT '',
  `address2` varchar(250) NOT NULL DEFAULT '',
  `landmark` varchar(250) NOT NULL DEFAULT '',
  `locality` varchar(250) NOT NULL DEFAULT '',
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `country_id` bigint(20) UNSIGNED DEFAULT NULL,
  `state_id` bigint(20) UNSIGNED DEFAULT NULL,
  `district_id` bigint(20) UNSIGNED DEFAULT NULL,
  `city_id` bigint(20) UNSIGNED DEFAULT NULL,
  `pincode_id` bigint(20) UNSIGNED DEFAULT NULL,
  `zipcode` varchar(250) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `model_type` varchar(255) DEFAULT NULL,
  `model_id` varchar(255) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `appraisals` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `weightage_id` bigint(20) DEFAULT NULL,
  `kra` varchar(191) DEFAULT NULL,
  `year` varchar(110) DEFAULT NULL,
  `target` int(11) DEFAULT NULL,
  `achivment` int(11) DEFAULT NULL,
  `acual` varchar(150) DEFAULT NULL,
  `rating` text DEFAULT NULL,
  `rating_by` text DEFAULT NULL,
  `appraisal_type` varchar(120) DEFAULT NULL,
  `appraisal_session` varchar(120) DEFAULT NULL,
  `promotion` text DEFAULT NULL,
  `remark` text DEFAULT NULL,
  `grade_percentage` int(11) DEFAULT NULL,
  `grade` varchar(120) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `attachments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `order_id` bigint(20) UNSIGNED DEFAULT NULL,
  `sales_id` bigint(20) UNSIGNED DEFAULT NULL,
  `file_path` varchar(450) NOT NULL DEFAULT '',
  `document_name` varchar(250) NOT NULL DEFAULT '',
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `attendances` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `punchin_date` date NOT NULL,
  `punchin_time` time NOT NULL,
  `punchin_longitude` varchar(250) DEFAULT NULL,
  `punchin_latitude` varchar(250) DEFAULT NULL,
  `punchin_address` varchar(250) NOT NULL DEFAULT '',
  `punchin_image` varchar(400) NOT NULL DEFAULT '',
  `punchout_date` date DEFAULT NULL,
  `punchout_time` time DEFAULT NULL,
  `punchout_latitude` varchar(250) DEFAULT NULL,
  `punchout_longitude` varchar(250) DEFAULT NULL,
  `punchout_address` varchar(250) NOT NULL DEFAULT '',
  `punchout_image` varchar(400) NOT NULL DEFAULT '',
  `punchin_summary` varchar(255) NOT NULL DEFAULT '',
  `punchout_summary` varchar(255) NOT NULL DEFAULT '',
  `flag` varchar(255) DEFAULT NULL,
  `worked_time` varchar(50) NOT NULL DEFAULT '',
  `attendance_status` tinyint(4) NOT NULL DEFAULT 0,
  `beat_id` varchar(125) DEFAULT NULL,
  `punchin_from` varchar(125) DEFAULT NULL,
  `remark_status` varchar(191) DEFAULT NULL,
  `created_by` varchar(191) DEFAULT NULL,
  `approve_reject_by` varchar(191) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `working_type` varchar(400) NOT NULL DEFAULT 'fields',
  `tourid` varchar(255) DEFAULT NULL,
  `city` varchar(3000) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `beats` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `beat_name` varchar(250) NOT NULL,
  `description` varchar(450) NOT NULL DEFAULT '',
  `region_id` bigint(20) UNSIGNED DEFAULT NULL,
  `country_id` bigint(20) UNSIGNED DEFAULT NULL,
  `state_id` bigint(20) UNSIGNED DEFAULT NULL,
  `district_id` varchar(225) DEFAULT NULL,
  `city_id` varchar(225) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `beat_customers` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `beat_id` bigint(20) UNSIGNED DEFAULT NULL,
  `distributor_id` bigint(20) DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_type` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `beat_schedules` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `beat_id` bigint(20) UNSIGNED DEFAULT NULL,
  `beat_date` date DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `tourid` bigint(20) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `beat_users` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `beat_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `branches` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `branch_name` varchar(250) NOT NULL,
  `branch_code` varchar(125) DEFAULT NULL,
  `warehouse_id` varchar(125) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `branchwise_targets` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_name` varchar(255) DEFAULT NULL,
  `branch_id` bigint(20) UNSIGNED DEFAULT NULL,
  `branch_name` varchar(255) DEFAULT NULL,
  `div_id` bigint(20) UNSIGNED DEFAULT NULL,
  `division_name` varchar(255) DEFAULT NULL,
  `month` varchar(255) DEFAULT NULL,
  `year` varchar(255) DEFAULT NULL,
  `target` decimal(19,2) NOT NULL DEFAULT 0.00,
  `achievement` decimal(19,2) NOT NULL DEFAULT 0.00,
  `type` varchar(255) DEFAULT NULL,
  `amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `branch_oprning_quantities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `item_code` varchar(255) DEFAULT NULL,
  `item_description` varchar(255) DEFAULT NULL,
  `item_group` varchar(255) DEFAULT NULL,
  `branch_id` varchar(255) DEFAULT NULL,
  `qty_month` date DEFAULT NULL,
  `open_order_qty` int(11) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `branch_stocks` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `branch_id` bigint(20) DEFAULT NULL,
  `warehouse_id` bigint(20) DEFAULT NULL,
  `branch_name` varchar(255) DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `amount` varchar(255) DEFAULT NULL,
  `days` varchar(255) DEFAULT NULL,
  `year` varchar(125) DEFAULT NULL,
  `quarter` varchar(125) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `brands` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `brand_name` varchar(250) NOT NULL,
  `brand_image` varchar(350) NOT NULL DEFAULT '',
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `call_logs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `lead_id` bigint(20) UNSIGNED NOT NULL,
  `number` varchar(255) DEFAULT NULL,
  `started_at` datetime NOT NULL COMMENT 'Call start date & time',
  `duration` int(11) NOT NULL DEFAULT 0 COMMENT 'Duration in seconds',
  `user_id` int(11) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0 COMMENT '0 = No Response, 1 = Received',
  `remark` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `categories` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `category_name` varchar(250) NOT NULL,
  `category_image` varchar(350) NOT NULL DEFAULT '',
  `sap_code` varchar(350) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `check_in` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `entity_type` varchar(255) DEFAULT NULL,
  `entity_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `checkin_date` date NOT NULL,
  `checkin_time` time NOT NULL,
  `checkin_latitude` varchar(250) DEFAULT NULL,
  `checkin_longitude` varchar(250) DEFAULT NULL,
  `checkin_address` varchar(250) DEFAULT NULL,
  `checkout_date` date DEFAULT NULL,
  `checkout_time` time DEFAULT NULL,
  `time_interval` time DEFAULT NULL,
  `checkout_latitude` varchar(250) DEFAULT NULL,
  `checkout_longitude` varchar(250) DEFAULT NULL,
  `checkout_address` varchar(250) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `distance` varchar(250) DEFAULT NULL,
  `beatscheduleid` bigint(20) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `check_in_drafts` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `checkin_id` bigint(20) DEFAULT NULL,
  `draft_msg` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `cities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `city_name` varchar(250) NOT NULL,
  `district_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `state_id` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `grade` varchar(50) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `claim_generations` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `service_center_id` bigint(20) DEFAULT NULL,
  `month` varchar(255) DEFAULT NULL,
  `year` varchar(255) DEFAULT NULL,
  `claim_number` varchar(255) DEFAULT NULL,
  `claim_amount` double(8,2) DEFAULT NULL,
  `courier_details` varchar(255) DEFAULT NULL,
  `courier_date` date DEFAULT NULL,
  `asc_bill_no` varchar(255) DEFAULT NULL,
  `asc_bill_date` date DEFAULT NULL,
  `asc_bill_amount` double DEFAULT NULL,
  `claim_sattlement_details` text DEFAULT NULL,
  `submitted_by_se` tinyint(4) DEFAULT NULL,
  `claim_approved` tinyint(4) DEFAULT NULL,
  `claim_done` tinyint(4) DEFAULT NULL,
  `claim_date` date DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `claim_generation_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `claim_generation_id` bigint(20) DEFAULT NULL,
  `complaint_id` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `complaints` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `complaint_number` varchar(255) DEFAULT NULL,
  `complaint_date` date DEFAULT NULL,
  `claim_amount` varchar(125) DEFAULT NULL,
  `complaint_status` int(11) NOT NULL DEFAULT 0,
  `seller` varchar(225) DEFAULT NULL,
  `end_user_id` bigint(20) DEFAULT NULL,
  `party_name` bigint(20) DEFAULT NULL,
  `product_laying` varchar(255) DEFAULT NULL,
  `service_center` bigint(20) DEFAULT NULL,
  `assign_user` bigint(20) DEFAULT NULL,
  `product_id` bigint(20) DEFAULT NULL,
  `product_serail_number` varchar(255) DEFAULT NULL,
  `product_code` varchar(255) DEFAULT NULL,
  `product_name` varchar(125) DEFAULT NULL,
  `category` varchar(255) DEFAULT NULL,
  `specification` varchar(255) DEFAULT NULL,
  `product_no` varchar(255) DEFAULT NULL,
  `phase` varchar(255) DEFAULT NULL,
  `seller_branch` varchar(255) DEFAULT NULL,
  `purchased_branch` varchar(255) DEFAULT NULL,
  `product_group` varchar(255) DEFAULT NULL,
  `company_sale_bill_no` varchar(255) DEFAULT NULL,
  `company_sale_bill_date` date DEFAULT NULL,
  `customer_bill_date` date DEFAULT NULL,
  `customer_bill_no` varchar(255) DEFAULT NULL,
  `company_bill_date_month` varchar(255) DEFAULT NULL,
  `under_warranty` varchar(255) DEFAULT NULL,
  `service_type` varchar(255) DEFAULT NULL,
  `customer_bill_date_month` varchar(255) DEFAULT NULL,
  `warranty_bill` varchar(255) DEFAULT NULL,
  `fault_type` varchar(255) DEFAULT NULL,
  `service_centre_remark` varchar(255) DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `division` varchar(255) DEFAULT NULL,
  `register_by` varchar(255) DEFAULT NULL,
  `complaint_type` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_by_device` varchar(125) DEFAULT NULL,
  `complaint_recieve_via` varchar(125) DEFAULT NULL,
  `created_by` int(11) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `complaint_timelines` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `complaint_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `complaint_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(255) NOT NULL DEFAULT 'Y',
  `name` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `complaint_work_dones` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `complaint_id` int(10) UNSIGNED NOT NULL,
  `done_by` varchar(255) NOT NULL,
  `remark` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `comp_off_leaves` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) DEFAULT NULL,
  `leave_id` varchar(225) DEFAULT NULL,
  `comp_off_date` date DEFAULT NULL,
  `expiry_date` date DEFAULT NULL,
  `is_used` tinyint(1) NOT NULL DEFAULT 0,
  `balance` decimal(19,2) NOT NULL DEFAULT 1.00,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `countries` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `country_name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `coupons` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `coupon` varchar(50) NOT NULL,
  `points` bigint(20) NOT NULL DEFAULT 0,
  `expiry_date` date DEFAULT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `coupon_profile_id` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `coupon_profiles` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `profile_name` varchar(250) NOT NULL,
  `coupon_length` varchar(250) NOT NULL DEFAULT '8',
  `excluding_character` varchar(450) NOT NULL DEFAULT '',
  `coupon_count` varchar(50) NOT NULL DEFAULT '',
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_custom_fields` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `field_name` varchar(255) NOT NULL,
  `field_key` varchar(255) DEFAULT NULL,
  `field_type` varchar(255) NOT NULL DEFAULT 'Input',
  `description` text DEFAULT NULL,
  `created_by` int(11) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_custom_field_values` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `custom_field_id` bigint(20) UNSIGNED NOT NULL,
  `value` varchar(255) NOT NULL,
  `sort_order` int(11) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `gstin_no` varchar(250) DEFAULT NULL,
  `pan_no` varchar(250) DEFAULT NULL,
  `aadhar_no` varchar(250) DEFAULT NULL,
  `account_holder` varchar(125) DEFAULT NULL,
  `account_number` varchar(125) DEFAULT NULL,
  `bank_name` varchar(125) DEFAULT NULL,
  `ifsc_code` varchar(125) DEFAULT NULL,
  `otherid_no` varchar(250) DEFAULT NULL,
  `gstin_no_status` tinyint(4) NOT NULL DEFAULT 0,
  `pan_no_status` tinyint(4) NOT NULL DEFAULT 0,
  `aadhar_no_status` tinyint(4) NOT NULL DEFAULT 0,
  `bank_status` tinyint(4) NOT NULL DEFAULT 0,
  `otherid_no_status` tinyint(4) NOT NULL DEFAULT 0,
  `status_update_by` bigint(20) DEFAULT NULL,
  `enrollment_date` datetime DEFAULT NULL,
  `approval_date` datetime DEFAULT NULL,
  `shop_image` varchar(250) NOT NULL DEFAULT '',
  `visiting_card` varchar(250) DEFAULT NULL,
  `grade` varchar(250) NOT NULL DEFAULT '',
  `visit_status` varchar(250) NOT NULL DEFAULT '',
  `fcm_token` text DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_outstantings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `branch_id` bigint(20) DEFAULT NULL,
  `customer_id` bigint(20) DEFAULT NULL,
  `user_id` bigint(20) DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `customer_name` varchar(255) DEFAULT NULL,
  `amount` varchar(255) DEFAULT NULL,
  `days` varchar(255) DEFAULT NULL,
  `year` varchar(125) DEFAULT NULL,
  `quarter` varchar(125) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_processes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `process_name` varchar(255) NOT NULL,
  `description` text DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_process_steps` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_process_id` bigint(20) UNSIGNED NOT NULL,
  `value` varchar(255) NOT NULL,
  `sort_order` int(11) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `customer_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `customertype_name` varchar(250) NOT NULL,
  `type_name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `custom_pdf_values` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `estimate_id` bigint(20) UNSIGNED NOT NULL,
  `label_id` bigint(20) UNSIGNED NOT NULL,
  `value` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `damage_entries` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) NOT NULL,
  `coupon_code` varchar(255) DEFAULT NULL,
  `point` int(11) DEFAULT 0,
  `scheme_id` bigint(20) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `remark` varchar(125) DEFAULT NULL,
  `created_by` bigint(20) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `dealer_appointments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `branch` varchar(255) DEFAULT NULL,
  `district` varchar(255) DEFAULT NULL,
  `city` varchar(125) DEFAULT NULL,
  `place` varchar(125) DEFAULT NULL,
  `appointment_date` varchar(255) DEFAULT NULL,
  `customertype` varchar(255) DEFAULT NULL,
  `old_user` varchar(125) DEFAULT NULL,
  `old_division` varchar(125) DEFAULT NULL,
  `old_firm_name` varchar(125) DEFAULT NULL,
  `old_gst` varchar(125) DEFAULT NULL,
  `division` varchar(255) DEFAULT NULL,
  `asc_divi` varchar(225) DEFAULT NULL,
  `parent_id` varchar(125) DEFAULT NULL,
  `security_deposit` varchar(125) DEFAULT NULL,
  `SDservicecenterd` bigint(20) DEFAULT NULL,
  `SDPUMPMOTORS` varchar(255) DEFAULT NULL,
  `SDF&A` varchar(255) DEFAULT NULL,
  `SDAGRI` varchar(125) NOT NULL DEFAULT '100000',
  `gst_type` varchar(255) DEFAULT NULL,
  `gst_no` varchar(255) DEFAULT NULL,
  `firm_type` varchar(255) DEFAULT NULL,
  `firm_name` varchar(255) DEFAULT NULL,
  `cin_no` varchar(255) DEFAULT NULL,
  `related_firm_name` varchar(255) DEFAULT NULL,
  `line_business` text DEFAULT NULL,
  `office_address` text DEFAULT NULL,
  `office_pincode` text DEFAULT NULL,
  `office_mobile` text DEFAULT NULL,
  `office_email` text DEFAULT NULL,
  `godown_address` text DEFAULT NULL,
  `godown_pincode` text DEFAULT NULL,
  `godown_mobile` text DEFAULT NULL,
  `godown_email` text DEFAULT NULL,
  `status` text DEFAULT NULL,
  `ppd_name_1` text DEFAULT NULL,
  `ppd_adhar_1` text DEFAULT NULL,
  `ppd_pan_1` text DEFAULT NULL,
  `ppd_name_2` text DEFAULT NULL,
  `ppd_adhar_2` text DEFAULT NULL,
  `ppd_pan_2` text DEFAULT NULL,
  `ppd_name_3` text DEFAULT NULL,
  `ppd_adhar_3` text DEFAULT NULL,
  `ppd_pan_3` text DEFAULT NULL,
  `ppd_name_4` text DEFAULT NULL,
  `ppd_adhar_4` text DEFAULT NULL,
  `ppd_pan_4` text DEFAULT NULL,
  `contact_person_name` text DEFAULT NULL,
  `mobile_email` text DEFAULT NULL,
  `bank_name` text DEFAULT NULL,
  `bank_address` text DEFAULT NULL,
  `account_type` text DEFAULT NULL,
  `account_number` text DEFAULT NULL,
  `ifsc_code` text DEFAULT NULL,
  `payment_term` text DEFAULT NULL,
  `credit_period` text DEFAULT NULL,
  `cheque_no_1` text DEFAULT NULL,
  `cheque_account_number_1` text DEFAULT NULL,
  `cheque_bank_1` text DEFAULT NULL,
  `cheque_no_2` text DEFAULT NULL,
  `cheque_account_number_2` text DEFAULT NULL,
  `cheque_bank_2` text DEFAULT NULL,
  `manufacture_company_1` text DEFAULT NULL,
  `manufacture_product_1` text DEFAULT NULL,
  `manufacture_business_1` text DEFAULT NULL,
  `manufacture_turn_over_1` text DEFAULT NULL,
  `manufacture_company_2` text DEFAULT NULL,
  `manufacture_product_2` text DEFAULT NULL,
  `manufacture_business_2` text DEFAULT NULL,
  `manufacture_turn_over_2` text DEFAULT NULL,
  `present_annual_turnover` text DEFAULT NULL,
  `motor_anticipated_business_1` text DEFAULT NULL,
  `motor_next_year_business_1` text DEFAULT NULL,
  `pump_anticipated_business_1` text DEFAULT NULL,
  `pump_next_year_business_1` text DEFAULT NULL,
  `F&A_anticipated_business_1` text DEFAULT NULL,
  `F&A_next_year_business_1` varchar(255) DEFAULT NULL,
  `lighting_anticipated_business_1` varchar(255) DEFAULT NULL,
  `lighting_next_year_business_1` varchar(255) DEFAULT NULL,
  `agri_anticipated_business_1` varchar(255) DEFAULT NULL,
  `agri_next_year_business_1` varchar(255) DEFAULT NULL,
  `solar_anticipated_business_1` varchar(255) DEFAULT NULL,
  `solar_next_year_business_1` varchar(255) DEFAULT NULL,
  `anticipated_business_total` varchar(255) DEFAULT NULL,
  `approval_status` tinyint(4) NOT NULL DEFAULT 0,
  `dealer_board` tinyint(4) NOT NULL DEFAULT 0,
  `welcome_kit` tinyint(4) NOT NULL DEFAULT 0,
  `sales_approve` int(11) DEFAULT NULL,
  `account_approve` int(11) DEFAULT NULL,
  `ho_approve` int(11) DEFAULT NULL,
  `ho_approve_date` date DEFAULT NULL,
  `board_install_date` date DEFAULT NULL,
  `welcome_kit_date` date DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `bm_remark` varchar(255) DEFAULT NULL,
  `bm_remark_user` int(11) DEFAULT NULL,
  `payment_term_bm` text DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `dealer_appointment_kycs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `appointment_id` bigint(20) DEFAULT NULL,
  `channel_partner` varchar(255) DEFAULT NULL,
  `place` varchar(125) DEFAULT NULL,
  `concerned_branch` varchar(255) DEFAULT NULL,
  `dealer_code` varchar(255) DEFAULT NULL,
  `division` varchar(255) DEFAULT NULL,
  `proprietary_concern` varchar(255) DEFAULT NULL,
  `partnership_firm` varchar(255) DEFAULT NULL,
  `ltd_pvt` varchar(255) DEFAULT NULL,
  `distribution_channel` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `dealer_portal_settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `slider` varchar(255) DEFAULT 'Y',
  `slider_heading` varchar(225) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `deal_ins` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `types` varchar(150) NOT NULL,
  `hcv` tinyint(1) NOT NULL DEFAULT 0,
  `mav` tinyint(1) NOT NULL DEFAULT 0,
  `lmv` tinyint(1) NOT NULL DEFAULT 0,
  `lcv` tinyint(1) NOT NULL DEFAULT 0,
  `other` tinyint(1) NOT NULL DEFAULT 0,
  `tractor` tinyint(1) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `departments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `designations` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `designation_name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `districts` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `district_name` varchar(250) NOT NULL,
  `state_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `divisions` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `division_name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `employee_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `end_users` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_name` varchar(255) DEFAULT NULL,
  `customer_number` varchar(255) DEFAULT NULL,
  `customer_email` varchar(255) DEFAULT NULL,
  `customer_address` varchar(255) DEFAULT NULL,
  `customer_place` varchar(255) DEFAULT NULL,
  `customer_pindcode` varchar(255) DEFAULT NULL,
  `customer_country` varchar(255) DEFAULT NULL,
  `customer_state` varchar(255) DEFAULT NULL,
  `state_id` int(11) DEFAULT NULL,
  `district_id` int(11) DEFAULT NULL,
  `city_id` int(11) DEFAULT NULL,
  `customer_district` varchar(255) DEFAULT NULL,
  `customer_city` varchar(255) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 1,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `estimates` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) UNSIGNED NOT NULL,
  `place_of_supply` varchar(255) DEFAULT NULL,
  `estimate_no` varchar(255) NOT NULL,
  `order_no` varchar(255) DEFAULT NULL,
  `estimate_date` date NOT NULL,
  `payment_term` varchar(255) DEFAULT NULL,
  `due_date` date DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `sub_total` decimal(15,2) NOT NULL DEFAULT 0.00,
  `discount_type` enum('amount','percentage') DEFAULT NULL,
  `discount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `discount_amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `tds` int(15) DEFAULT 0,
  `tds_amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `adjustment` decimal(15,2) NOT NULL DEFAULT 0.00,
  `grand_total` decimal(15,2) NOT NULL DEFAULT 0.00,
  `customer_notes` text DEFAULT NULL,
  `t_c` text DEFAULT NULL,
  `invoice_id` int(11) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `estimate_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `estimate_id` bigint(20) UNSIGNED NOT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `product_dec` varchar(255) DEFAULT NULL,
  `hsn_sac` varchar(255) DEFAULT NULL,
  `hsn_sac_type` varchar(255) DEFAULT NULL,
  `quantity` int(11) NOT NULL DEFAULT 1,
  `mrp` decimal(15,2) NOT NULL DEFAULT 0.00,
  `tax` int(22) DEFAULT NULL,
  `tax_amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `expenses` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `expenses_type` bigint(20) DEFAULT NULL,
  `user_id` bigint(20) DEFAULT NULL,
  `date` varchar(255) DEFAULT NULL,
  `claim_amount` double(8,2) DEFAULT NULL,
  `approve_amount` double(10,2) DEFAULT NULL,
  `start_km` varchar(255) DEFAULT NULL,
  `stop_km` varchar(255) DEFAULT NULL,
  `total_km` varchar(255) DEFAULT NULL,
  `note` text DEFAULT NULL,
  `checker_status` tinyint(4) NOT NULL DEFAULT 0,
  `accountant_status` tinyint(4) NOT NULL DEFAULT 0,
  `approve_reject_by` bigint(20) DEFAULT NULL,
  `reason` text DEFAULT NULL,
  `created_by` bigint(20) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `expenses_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) NOT NULL,
  `rate` double(8,2) NOT NULL DEFAULT 0.00,
  `is_active` tinyint(4) NOT NULL DEFAULT 1,
  `allowance_type_id` bigint(20) NOT NULL,
  `payroll_id` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `expense_logs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `log_date` date DEFAULT NULL,
  `expense_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `status_type` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `failed_jobs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `uuid` varchar(255) NOT NULL,
  `connection` text NOT NULL,
  `queue` text NOT NULL,
  `payload` longtext NOT NULL,
  `exception` longtext NOT NULL,
  `failed_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `fields` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `field_name` varchar(250) NOT NULL DEFAULT '',
  `field_type` varchar(250) NOT NULL DEFAULT '',
  `is_required` varchar(10) NOT NULL DEFAULT 'false',
  `is_multiple` varchar(10) NOT NULL DEFAULT 'false',
  `label_name` varchar(250) NOT NULL DEFAULT '',
  `placeholder` varchar(250) NOT NULL DEFAULT '',
  `module` varchar(250) NOT NULL DEFAULT '',
  `division_id` bigint(20) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `fieldsdata` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `field_id` bigint(20) UNSIGNED DEFAULT NULL,
  `value` varchar(250) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `field_konnect_app_settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `app_version` varchar(255) NOT NULL,
  `order_discount_limit` int(11) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `firm_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `firmtype_name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `gamifications` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `type` varchar(150) NOT NULL,
  `points` bigint(20) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `geo_locator_settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_filter` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`customer_filter`)),
  `lead_filter` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`lead_filter`)),
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `gifts` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `product_name` varchar(250) NOT NULL,
  `display_name` varchar(250) NOT NULL DEFAULT '',
  `description` varchar(450) NOT NULL DEFAULT '',
  `product_image` varchar(300) NOT NULL DEFAULT '',
  `mrp` decimal(8,2) NOT NULL DEFAULT 0.00,
  `price` decimal(8,2) NOT NULL DEFAULT 0.00,
  `points` bigint(20) NOT NULL DEFAULT 0,
  `subcategory_id` bigint(20) UNSIGNED DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `brand_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_type_id` bigint(20) DEFAULT NULL,
  `unit_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `giftsubcategories` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `subcategory_name` varchar(250) NOT NULL,
  `subcategory_image` varchar(350) NOT NULL DEFAULT '',
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `gift_brands` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `brand_name` varchar(250) NOT NULL,
  `brand_image` varchar(350) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `gift_categories` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `category_name` varchar(250) NOT NULL,
  `category_image` varchar(350) NOT NULL DEFAULT '',
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `gift_models` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `model_name` varchar(250) NOT NULL,
  `model_image` varchar(350) NOT NULL DEFAULT '',
  `sub_category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `gift_redemption_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `redemption_id` bigint(20) NOT NULL,
  `redemption_no` varchar(255) DEFAULT NULL,
  `purchase_rate` varchar(255) DEFAULT NULL,
  `gst` varchar(255) DEFAULT NULL,
  `total_purchase` varchar(255) DEFAULT NULL,
  `purchase_invoice_no` varchar(255) DEFAULT NULL,
  `purchase_return_no` varchar(255) DEFAULT NULL,
  `client_invoice_no` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `holidays` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `name2` varchar(250) DEFAULT NULL,
  `name` longtext DEFAULT NULL,
  `holiday_date2` varchar(250) DEFAULT NULL,
  `holiday_date` longtext DEFAULT NULL,
  `branch` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `invoices` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) UNSIGNED NOT NULL,
  `place_of_supply` varchar(255) DEFAULT NULL,
  `invoice_no` varchar(255) NOT NULL,
  `order_no` varchar(255) DEFAULT NULL,
  `invoice_date` date NOT NULL,
  `payment_term` varchar(255) DEFAULT NULL,
  `due_date` date DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `sub_total` decimal(15,2) NOT NULL DEFAULT 0.00,
  `discount_type` enum('amount','percentage') DEFAULT NULL,
  `discount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `discount_amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `tds` int(15) DEFAULT 0,
  `tds_amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `adjustment` decimal(15,2) NOT NULL DEFAULT 0.00,
  `grand_total` decimal(15,2) NOT NULL DEFAULT 0.00,
  `customer_notes` text DEFAULT NULL,
  `t_c` text DEFAULT NULL,
  `paid_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `status_id` tinyint(4) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `invoice_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `invoice_id` bigint(20) UNSIGNED NOT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `product_dec` varchar(255) DEFAULT NULL,
  `hsn_sac` varchar(255) DEFAULT NULL,
  `hsn_sac_type` varchar(255) DEFAULT NULL,
  `quantity` int(11) NOT NULL DEFAULT 1,
  `mrp` decimal(15,2) NOT NULL DEFAULT 0.00,
  `tax` int(22) NOT NULL DEFAULT 0,
  `tax_amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `amount` decimal(15,2) NOT NULL DEFAULT 0.00,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `invoice_labels` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `invoice_setting_id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) NOT NULL,
  `page_heading` varchar(255) DEFAULT NULL,
  `icon` varchar(255) DEFAULT NULL,
  `page` tinyint(4) NOT NULL CHECK (`page` between 2 and 5),
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `invoice_settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `invoice_logo` varchar(255) DEFAULT NULL,
  `invoice_esign` varchar(255) DEFAULT NULL,
  `company_name` varchar(255) DEFAULT NULL,
  `gst_number` varchar(255) DEFAULT NULL,
  `pan_number` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `jobs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `queue` varchar(255) NOT NULL,
  `payload` longtext NOT NULL,
  `attempts` tinyint(3) UNSIGNED NOT NULL,
  `reserved_at` int(10) UNSIGNED DEFAULT NULL,
  `available_at` int(10) UNSIGNED NOT NULL,
  `created_at` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `leads` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `company_name` varchar(255) NOT NULL,
  `company_url` varchar(255) DEFAULT NULL,
  `address_id` varchar(255) DEFAULT NULL,
  `lead_source` varchar(255) DEFAULT NULL,
  `status` varchar(255) NOT NULL DEFAULT '0',
  `assign_to` int(22) DEFAULT NULL,
  `lead_generation_date` date DEFAULT NULL,
  `conversion_date` date DEFAULT NULL,
  `customer_id` int(11) DEFAULT NULL,
  `on_location` tinyint(1) NOT NULL DEFAULT 0,
  `latitude` varchar(50) DEFAULT NULL,
  `longitude` varchar(50) DEFAULT NULL,
  `location_address` varchar(255) DEFAULT NULL,
  `others` text DEFAULT NULL,
  `created_by` int(191) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_check_in` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `lead_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `checkin_date` date NOT NULL,
  `checkin_time` time NOT NULL,
  `checkin_latitude` varchar(250) DEFAULT NULL,
  `checkin_longitude` varchar(250) DEFAULT NULL,
  `checkin_address` varchar(250) DEFAULT NULL,
  `checkout_date` date DEFAULT NULL,
  `checkout_time` time DEFAULT NULL,
  `time_interval` time DEFAULT NULL,
  `checkout_latitude` varchar(250) DEFAULT NULL,
  `checkout_longitude` varchar(250) DEFAULT NULL,
  `checkout_address` varchar(250) DEFAULT NULL,
  `checkout_note` varchar(255) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `distance` varchar(250) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_contacts` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `lead_id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `title` varchar(255) DEFAULT NULL,
  `phone_number` varchar(255) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `lead_source` varchar(255) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `created_by` int(191) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_logs` (
  `id` int(10) UNSIGNED NOT NULL,
  `lead_id` int(10) UNSIGNED NOT NULL,
  `message` text NOT NULL,
  `created_by` int(10) UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_notes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `lead_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `note` longtext DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_notifications` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `model_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `title` varchar(255) NOT NULL,
  `body` text NOT NULL,
  `model` varchar(255) NOT NULL DEFAULT 'lead',
  `read` tinyint(1) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_opportunities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `lead_id` bigint(20) UNSIGNED DEFAULT NULL,
  `assigned_to` bigint(20) UNSIGNED DEFAULT NULL,
  `lead_contact_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `amount` double(10,2) NOT NULL DEFAULT 0.00,
  `type` varchar(255) DEFAULT NULL,
  `estimated_close_date` date DEFAULT NULL,
  `confidence` int(11) NOT NULL DEFAULT 0 COMMENT 'Confidence level from 0 to 100 in percentage',
  `note` longtext DEFAULT NULL,
  `status` varchar(191) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `lead_tasks` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `lead_id` bigint(20) UNSIGNED DEFAULT NULL,
  `assigned_to` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `description` longtext DEFAULT NULL,
  `date` date DEFAULT NULL,
  `time` time DEFAULT NULL,
  `priority` varchar(255) DEFAULT NULL,
  `status` varchar(255) NOT NULL DEFAULT 'pending',
  `open_date` date DEFAULT NULL,
  `due_date` date DEFAULT NULL,
  `close_date` date DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `leaves` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `from_date` date NOT NULL,
  `to_date` date NOT NULL,
  `type` varchar(255) NOT NULL DEFAULT 'leave',
  `bal_type` varchar(255) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `reason` varchar(255) NOT NULL DEFAULT '',
  `remark_status` varchar(125) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `loyalty_app_settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_types` text DEFAULT NULL,
  `app_version` float NOT NULL DEFAULT 1.01,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `marketings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `event_date` date DEFAULT NULL,
  `event_center` varchar(255) DEFAULT NULL,
  `division` varchar(255) DEFAULT NULL,
  `place_of_participant` varchar(255) DEFAULT NULL,
  `event_district` varchar(255) DEFAULT NULL,
  `state` varchar(255) DEFAULT NULL,
  `event_under_type` varchar(255) DEFAULT NULL,
  `event_under_name` varchar(255) DEFAULT NULL,
  `branch` varchar(255) DEFAULT NULL,
  `responsible_for_event` varchar(255) DEFAULT NULL,
  `branding_team_member` varchar(255) DEFAULT NULL,
  `name_of_participant` varchar(255) DEFAULT NULL,
  `category_of_participant` varchar(255) DEFAULT NULL,
  `mob_no_of_participant` varchar(255) DEFAULT NULL,
  `count_of_participant` bigint(20) DEFAULT NULL,
  `google_drivelink` varchar(255) DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `marketing_activities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `slug` varchar(255) DEFAULT NULL,
  `type` varchar(255) DEFAULT NULL,
  `activity_division` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `market_intelligences_fielddatas` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `field_id` bigint(20) UNSIGNED DEFAULT NULL,
  `value` varchar(250) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `market_intelligences_fields` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `field_name` varchar(250) NOT NULL DEFAULT '',
  `field_type` varchar(250) NOT NULL DEFAULT '',
  `is_required` varchar(10) NOT NULL DEFAULT 'false',
  `is_multiple` varchar(10) NOT NULL DEFAULT 'false',
  `label_name` varchar(250) DEFAULT '',
  `placeholder` varchar(250) DEFAULT '',
  `key` varchar(250) DEFAULT NULL,
  `module` varchar(250) DEFAULT '',
  `input_type` varchar(250) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `division_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `market_intelligence_serveys` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `title` varchar(125) DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `market_intelligence_servey_data` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `servey_id` bigint(20) UNSIGNED DEFAULT NULL,
  `key` varchar(255) DEFAULT NULL,
  `value` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `master_distributors` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `legal_name` varchar(255) NOT NULL,
  `trade_name` varchar(255) DEFAULT NULL,
  `distributor_code` varchar(255) NOT NULL,
  `category` varchar(255) NOT NULL,
  `business_status` varchar(255) NOT NULL,
  `business_start_date` date NOT NULL,
  `shop_image` varchar(255) DEFAULT NULL,
  `profile_image` varchar(255) DEFAULT NULL,
  `contact_person` varchar(255) NOT NULL,
  `designation` varchar(255) DEFAULT NULL,
  `mobile` varchar(255) NOT NULL,
  `alternate_mobile` varchar(255) DEFAULT NULL,
  `email` varchar(255) NOT NULL,
  `secondary_email` varchar(255) DEFAULT NULL,
  `billing_address` varchar(255) NOT NULL,
  `billing_city` varchar(255) DEFAULT NULL,
  `billing_district` varchar(255) DEFAULT NULL,
  `billing_state` varchar(255) DEFAULT NULL,
  `billing_country` varchar(255) DEFAULT NULL,
  `billing_pincode` varchar(255) DEFAULT NULL,
  `shipping_address` varchar(255) DEFAULT NULL,
  `shipping_city` varchar(255) DEFAULT NULL,
  `shipping_district` varchar(255) DEFAULT NULL,
  `shipping_state` varchar(255) DEFAULT NULL,
  `shipping_country` varchar(255) DEFAULT NULL,
  `shipping_pincode` varchar(255) DEFAULT NULL,
  `sales_zone` varchar(255) NOT NULL,
  `area_territory` varchar(255) NOT NULL,
  `beat_route` varchar(255) DEFAULT NULL,
  `market_classification` varchar(255) NOT NULL,
  `competitor_brands` text DEFAULT NULL,
  `gst_number` varchar(255) NOT NULL,
  `pan_number` varchar(255) NOT NULL,
  `registration_type` varchar(255) NOT NULL,
  `documents` varchar(255) DEFAULT NULL,
  `bank_name` varchar(255) NOT NULL,
  `account_holder` varchar(255) NOT NULL,
  `account_number` varchar(255) NOT NULL,
  `ifsc` varchar(255) NOT NULL,
  `branch_name` varchar(255) DEFAULT NULL,
  `credit_limit` decimal(15,2) NOT NULL,
  `credit_days` int(11) NOT NULL,
  `avg_monthly_purchase` decimal(15,2) DEFAULT NULL,
  `outstanding_balance` decimal(15,2) DEFAULT NULL,
  `preferred_payment_method` varchar(255) DEFAULT NULL,
  `cancelled_cheque` varchar(255) NOT NULL,
  `monthly_sales` decimal(15,2) NOT NULL,
  `product_categories` varchar(255) NOT NULL,
  `secondary_sales_required` varchar(255) DEFAULT NULL,
  `last_12_months_sales` varchar(255) DEFAULT NULL,
  `sales_executive_id` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`sales_executive_id`)),
  `supervisor_id` bigint(20) UNSIGNED NOT NULL,
  `customer_segment` varchar(255) NOT NULL,
  `weekly_tai_alert` varchar(255) NOT NULL,
  `target_vs_achievement` varchar(255) NOT NULL,
  `schemes_updates` varchar(255) NOT NULL,
  `new_launch_update` varchar(255) NOT NULL,
  `payment_alert` varchar(255) NOT NULL,
  `pending_orders` varchar(255) NOT NULL,
  `inventory_status` varchar(255) NOT NULL,
  `turnover` decimal(15,2) NOT NULL,
  `staff_strength` varchar(255) NOT NULL,
  `vehicles_capacity` varchar(255) NOT NULL,
  `area_coverage` varchar(255) NOT NULL,
  `other_brands_handled` varchar(255) NOT NULL,
  `warehouse_size` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `mou_file` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `same_as_billing` tinyint(1) NOT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `beat_id` bigint(20) DEFAULT NULL,
  `gps_location` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `media` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `model_type` varchar(255) NOT NULL,
  `model_id` bigint(20) UNSIGNED NOT NULL,
  `uuid` char(36) DEFAULT NULL,
  `collection_name` varchar(255) NOT NULL,
  `name` varchar(255) NOT NULL,
  `file_name` varchar(255) NOT NULL,
  `mime_type` varchar(255) DEFAULT NULL,
  `disk` varchar(255) NOT NULL,
  `conversions_disk` varchar(255) DEFAULT NULL,
  `size` bigint(20) UNSIGNED NOT NULL,
  `manipulations` text NOT NULL,
  `custom_properties` text NOT NULL,
  `generated_conversions` text NOT NULL,
  `responsive_images` text NOT NULL,
  `order_column` int(10) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `migrations` (
  `id` int(10) UNSIGNED NOT NULL,
  `migration` varchar(255) NOT NULL,
  `batch` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `msp_activities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `emp_code` varchar(255) DEFAULT NULL,
  `activity_date` date DEFAULT NULL,
  `fyear` varchar(255) DEFAULT NULL,
  `month` varchar(255) DEFAULT NULL,
  `msp_count` bigint(20) DEFAULT NULL,
  `activity_type` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `msp_activity_cities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `msp_activity_id` bigint(20) DEFAULT NULL,
  `city_id` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `msp_activity_customers` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `msp_activity_id` bigint(20) DEFAULT NULL,
  `customer_id` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `neft_redemption_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `redemption_id` bigint(20) UNSIGNED NOT NULL,
  `utr_number` varchar(255) DEFAULT NULL,
  `tds` int(11) DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `new_invoices` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `secondary_customer_id` bigint(20) UNSIGNED NOT NULL,
  `invoice_number` varchar(255) NOT NULL,
  `invoice_date` date NOT NULL,
  `amount` decimal(15,2) NOT NULL,
  `points` decimal(15,2) NOT NULL DEFAULT 0.00,
  `approval_status` tinyint(4) NOT NULL DEFAULT 0,
  `approval_remark` text DEFAULT NULL,
  `approved_ss_by` bigint(20) UNSIGNED DEFAULT NULL,
  `approved_ss_at` timestamp NULL DEFAULT NULL,
  `approved_sales_by` bigint(20) UNSIGNED DEFAULT NULL,
  `approved_sales_at` timestamp NULL DEFAULT NULL,
  `approved_ho_by` bigint(20) UNSIGNED DEFAULT NULL,
  `approved_ho_at` timestamp NULL DEFAULT NULL,
  `rejected_by` bigint(20) UNSIGNED DEFAULT NULL,
  `rejected_at` timestamp NULL DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `new_invoice_approval_logs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `log_date` date DEFAULT NULL,
  `new_invoice_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `status_type` varchar(255) DEFAULT NULL,
  `from_status` tinyint(4) DEFAULT NULL,
  `to_status` tinyint(4) DEFAULT NULL,
  `remark` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `new_joinings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `email` varchar(255) DEFAULT NULL,
  `first_name` varchar(255) DEFAULT NULL,
  `middle_name` varchar(255) DEFAULT NULL,
  `last_name` varchar(255) DEFAULT NULL,
  `gender` varchar(255) DEFAULT NULL,
  `dob` date DEFAULT NULL,
  `mobile_number` varchar(255) DEFAULT NULL,
  `contact_number` varchar(255) DEFAULT NULL,
  `father_name` varchar(255) DEFAULT NULL,
  `father_occupation` varchar(255) DEFAULT NULL,
  `mother_name` varchar(255) DEFAULT NULL,
  `mother_occupation` varchar(255) DEFAULT NULL,
  `marital_status` varchar(255) DEFAULT NULL,
  `spouse_name` varchar(255) DEFAULT NULL,
  `spouse_dob` date DEFAULT NULL,
  `spouse_education` varchar(255) DEFAULT NULL,
  `spouse_occupation` varchar(255) DEFAULT NULL,
  `anniversary` varchar(255) DEFAULT NULL,
  `present_address` varchar(255) DEFAULT NULL,
  `present_city` varchar(255) DEFAULT NULL,
  `present_state` varchar(255) DEFAULT NULL,
  `present_pincode` varchar(255) DEFAULT NULL,
  `permanent_address` varchar(255) DEFAULT NULL,
  `permanent_city` varchar(255) DEFAULT NULL,
  `permanent_state` varchar(255) DEFAULT NULL,
  `permanent_pincode` varchar(255) DEFAULT NULL,
  `pan` varchar(255) DEFAULT NULL,
  `aadhar` varchar(255) DEFAULT NULL,
  `driving_licence` varchar(255) DEFAULT NULL,
  `blood_group` varchar(255) DEFAULT NULL,
  `language` varchar(255) DEFAULT NULL,
  `other_language` varchar(255) DEFAULT NULL,
  `qualification` varchar(255) DEFAULT NULL,
  `experience` varchar(255) DEFAULT NULL,
  `skill` varchar(255) DEFAULT NULL,
  `occupy` varchar(255) DEFAULT NULL,
  `branch` varchar(255) DEFAULT NULL,
  `department` varchar(255) DEFAULT NULL,
  `date_of_joining` date DEFAULT NULL,
  `designation` varchar(255) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `notes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `note` text DEFAULT NULL,
  `purpose` varchar(250) NOT NULL DEFAULT '',
  `callstatus` varchar(450) NOT NULL DEFAULT '',
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `notifications` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `type` varchar(250) NOT NULL DEFAULT '',
  `data` text DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `oauth_auth_codes` (
  `id` varchar(100) NOT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `client_id` char(36) NOT NULL,
  `scopes` text DEFAULT NULL,
  `revoked` tinyint(1) NOT NULL,
  `expires_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `oauth_clients` (
  `id` char(36) NOT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `name` varchar(255) NOT NULL,
  `secret` varchar(100) DEFAULT NULL,
  `provider` varchar(255) DEFAULT NULL,
  `redirect` text NOT NULL,
  `personal_access_client` tinyint(1) NOT NULL,
  `password_client` tinyint(1) NOT NULL,
  `revoked` tinyint(1) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `oauth_personal_access_clients` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `client_id` char(36) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `oauth_refresh_tokens` (
  `id` varchar(100) NOT NULL,
  `access_token_id` varchar(100) NOT NULL,
  `revoked` tinyint(1) NOT NULL,
  `expires_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `opening_stocks` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `item_code` varchar(255) DEFAULT NULL,
  `item_description` varchar(255) DEFAULT NULL,
  `item_group` varchar(255) DEFAULT NULL,
  `ware_house_name` varchar(255) DEFAULT NULL,
  `branch_id` varchar(255) DEFAULT NULL,
  `opening_stocks` int(11) NOT NULL DEFAULT 0,
  `open_order_qty` int(11) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `opportunitie_statuses` (
  `id` int(11) NOT NULL,
  `status_name` varchar(255) DEFAULT NULL,
  `ordering` int(22) NOT NULL DEFAULT 1,
  `created_by` int(22) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `orders` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `buyer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `seller_id` bigint(20) UNSIGNED DEFAULT NULL,
  `executive_id` bigint(20) DEFAULT NULL,
  `total_qty` bigint(20) NOT NULL DEFAULT 0,
  `shipped_qty` bigint(20) NOT NULL DEFAULT 0,
  `orderno` varchar(250) NOT NULL DEFAULT '',
  `order_date` date DEFAULT NULL,
  `completed_date` datetime DEFAULT NULL,
  `estimated_date` date DEFAULT NULL,
  `total_gst` decimal(19,2) NOT NULL DEFAULT 0.00,
  `total_discount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `total_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `extra_discount` decimal(8,2) NOT NULL DEFAULT 0.00,
  `extra_discount_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `sub_total` decimal(19,2) NOT NULL DEFAULT 0.00,
  `grand_total` decimal(19,2) NOT NULL DEFAULT 0.00,
  `order_taking` varchar(250) NOT NULL DEFAULT '',
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `address_id` bigint(20) UNSIGNED DEFAULT NULL,
  `suc_del` varchar(191) DEFAULT NULL,
  `gst_amount` varchar(125) DEFAULT NULL,
  `schme_amount` decimal(19,2) DEFAULT NULL,
  `schme_val` decimal(19,2) DEFAULT NULL,
  `ebd_amount` decimal(19,2) DEFAULT NULL,
  `ebd_discount` decimal(19,2) DEFAULT NULL,
  `special_discount` int(11) DEFAULT NULL,
  `special_amount` decimal(19,2) DEFAULT NULL,
  `cluster_discount` int(11) DEFAULT NULL,
  `cluster_amount` decimal(19,2) DEFAULT NULL,
  `deal_discount` int(11) DEFAULT NULL,
  `deal_amount` decimal(19,2) DEFAULT NULL,
  `distributor_discount` int(11) DEFAULT NULL,
  `distributor_amount` decimal(19,2) DEFAULT NULL,
  `frieght_discount` int(11) DEFAULT NULL,
  `frieght_amount` decimal(19,2) DEFAULT NULL,
  `product_cat_id` int(11) DEFAULT NULL,
  `dod_discount` decimal(10,2) DEFAULT 0.00,
  `special_distribution_discount` decimal(10,2) DEFAULT 0.00,
  `distribution_margin_discount` decimal(10,2) DEFAULT 0.00,
  `total_fan_discount` decimal(10,2) DEFAULT 0.00,
  `total_fan_discount_amount` decimal(10,2) DEFAULT 0.00,
  `dod_discount_amount` decimal(19,2) DEFAULT 0.00,
  `special_distribution_discount_amount` decimal(19,2) DEFAULT 0.00,
  `distribution_margin_discount_amount` decimal(19,2) DEFAULT 0.00,
  `fan_extra_discount` decimal(19,2) DEFAULT 0.00,
  `fan_extra_discount_amount` decimal(19,2) DEFAULT 0.00,
  `cash_discount` int(11) DEFAULT 0,
  `cash_amount` decimal(10,2) DEFAULT 0.00,
  `agri_standard_discount` decimal(10,2) DEFAULT 0.00,
  `agri_standard_discount_amount` decimal(10,2) DEFAULT 0.00,
  `advance` decimal(19,2) DEFAULT 0.00,
  `gst5_amt` decimal(10,2) DEFAULT 0.00,
  `gst12_amt` decimal(10,2) DEFAULT 0.00,
  `gst18_amt` decimal(10,2) DEFAULT 0.00,
  `gst28_amt` decimal(10,2) DEFAULT 0.00,
  `order_remark` varchar(255) DEFAULT NULL,
  `discount_status` tinyint(4) NOT NULL DEFAULT 0,
  `sp_discount_status` tinyint(4) NOT NULL DEFAULT 0,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `beatscheduleid` bigint(20) UNSIGNED DEFAULT NULL,
  `order_type` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `order_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `order_id` bigint(20) UNSIGNED DEFAULT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `product_detail_id` bigint(20) UNSIGNED DEFAULT NULL,
  `quantity` bigint(20) NOT NULL DEFAULT 0,
  `shipped_qty` bigint(20) NOT NULL DEFAULT 0,
  `price` decimal(19,2) NOT NULL DEFAULT 0.00,
  `discount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `gst` decimal(19,2) NOT NULL DEFAULT 0.00,
  `gst_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `discount_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `tax_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `line_total` decimal(19,2) NOT NULL DEFAULT 0.00,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `scheme_name` varchar(255) DEFAULT NULL,
  `scheme_discount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `scheme_amount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `cluster_discount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `cluster_amount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `deal_discount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `deal_amount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `distributor_discount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `distributor_amount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `frieght_discount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `frieght_amount` decimal(10,2) NOT NULL DEFAULT 0.00,
  `cash_dis` decimal(10,2) NOT NULL DEFAULT 0.00,
  `cash_amounts` decimal(10,2) NOT NULL DEFAULT 0.00,
  `agri_standard_dis` decimal(10,2) DEFAULT 0.00,
  `agri_standard_dis_amounts` decimal(10,2) DEFAULT 0.00,
  `scheme_type` varchar(191) DEFAULT NULL,
  `scheme_value_type` varchar(191) DEFAULT NULL,
  `minimum` bigint(20) NOT NULL DEFAULT 0,
  `maximum` bigint(20) NOT NULL DEFAULT 0,
  `ebd_dis` int(11) DEFAULT NULL,
  `special_dis` int(11) DEFAULT NULL,
  `special_amounts` decimal(10,2) DEFAULT NULL,
  `ebd_amount` decimal(10,2) DEFAULT NULL,
  `start_date` varchar(191) DEFAULT NULL,
  `end_date` varchar(191) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `subcategory_id` int(20) DEFAULT NULL,
  `category_id` int(20) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `order_schemes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `scheme_name` varchar(250) NOT NULL,
  `scheme_description` longtext DEFAULT NULL,
  `start_date` date NOT NULL,
  `end_date` date NOT NULL,
  `repetition` int(11) DEFAULT NULL,
  `day_repeat` longtext DEFAULT NULL,
  `week_repeat` longtext DEFAULT NULL,
  `customer_type` varchar(255) DEFAULT NULL,
  `scheme_type` varchar(200) DEFAULT NULL,
  `scheme_basedon` varchar(200) DEFAULT NULL,
  `assign_to` varchar(200) DEFAULT NULL,
  `branch` varchar(255) DEFAULT NULL,
  `state` varchar(255) DEFAULT NULL,
  `customer` varchar(255) DEFAULT NULL,
  `minimum` bigint(20) DEFAULT NULL,
  `maximum` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `order_scheme_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `order_scheme_id` bigint(20) UNSIGNED NOT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `subcategory_id` bigint(20) UNSIGNED DEFAULT NULL,
  `points` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `parent_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `customer_id` bigint(20) DEFAULT NULL,
  `parent_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `password_resets` (
  `email` varchar(255) NOT NULL,
  `token` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `payments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_name` varchar(200) NOT NULL DEFAULT '',
  `payment_date` date DEFAULT NULL,
  `payment_mode` varchar(200) NOT NULL DEFAULT '',
  `payment_type` varchar(200) NOT NULL DEFAULT '',
  `bank_name` varchar(200) NOT NULL DEFAULT '',
  `reference_no` varchar(200) NOT NULL DEFAULT '',
  `amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `response` varchar(500) NOT NULL DEFAULT '',
  `description` varchar(500) NOT NULL DEFAULT '',
  `file_path` varchar(500) NOT NULL DEFAULT '',
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `payment_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `payment_id` bigint(20) UNSIGNED DEFAULT NULL,
  `sales_id` bigint(20) UNSIGNED DEFAULT NULL,
  `invoice_no` varchar(200) NOT NULL DEFAULT '',
  `amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `payment_terms` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `term_name` varchar(255) NOT NULL,
  `number_of_days` int(11) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `personal_access_tokens` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `tokenable_type` varchar(255) NOT NULL,
  `tokenable_id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) NOT NULL,
  `token` varchar(64) NOT NULL,
  `abilities` text DEFAULT NULL,
  `last_used_at` timestamp NULL DEFAULT NULL,
  `expires_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `pincodes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `pincode` varchar(250) NOT NULL,
  `city_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `planned_sop_sale_data` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `planned_sop_id` bigint(20) DEFAULT NULL,
  `month_1` varchar(255) DEFAULT NULL,
  `month_2` varchar(255) DEFAULT NULL,
  `month_3` varchar(255) DEFAULT NULL,
  `month_4` varchar(255) DEFAULT NULL,
  `month_5` varchar(255) DEFAULT NULL,
  `month_6` varchar(255) DEFAULT NULL,
  `month_7` varchar(255) DEFAULT NULL,
  `month_8` varchar(255) DEFAULT NULL,
  `month_9` varchar(255) DEFAULT NULL,
  `month_10` varchar(255) DEFAULT NULL,
  `month_11` varchar(255) DEFAULT NULL,
  `month_12` varchar(255) DEFAULT NULL,
  `min` varchar(255) DEFAULT NULL,
  `max` varchar(255) DEFAULT NULL,
  `avg` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `planned_s_o_p_s` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `planning_month` date DEFAULT NULL,
  `order_id` varchar(100) DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `branch_id` bigint(20) DEFAULT NULL,
  `product_id` bigint(20) DEFAULT NULL,
  `opening_stock` varchar(255) DEFAULT NULL,
  `open_order_qty` varchar(255) DEFAULT '0',
  `production_qty` varchar(255) DEFAULT '0',
  `plan_next_month` int(11) DEFAULT NULL,
  `budget_for_month` bigint(20) DEFAULT NULL,
  `last_month_sale` bigint(20) DEFAULT NULL,
  `last_three_month_avg` bigint(20) DEFAULT NULL,
  `last_year_month_sale` bigint(20) DEFAULT NULL,
  `sku_unit_price` int(11) DEFAULT NULL,
  `s_op_val` int(11) DEFAULT NULL,
  `top_sku` varchar(255) DEFAULT NULL,
  `plan_next_month_value` varchar(255) DEFAULT NULL,
  `dispatch_against_plan` int(11) DEFAULT 0,
  `created_by` varchar(255) DEFAULT NULL,
  `verify_by` varchar(255) DEFAULT NULL,
  `view_only` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `status` tinyint(4) DEFAULT 1,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `power_bi_settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `key` varchar(255) DEFAULT NULL,
  `value` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `primary_sales` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) DEFAULT 'Y',
  `invoiceno` varchar(250) DEFAULT '',
  `invoice_date` date DEFAULT NULL,
  `month` varchar(50) DEFAULT '',
  `division` varchar(50) DEFAULT '',
  `dealer` varchar(125) DEFAULT '',
  `customer_id` varchar(125) DEFAULT NULL,
  `city` varchar(50) DEFAULT '',
  `state` varchar(50) DEFAULT '',
  `final_branch` varchar(250) DEFAULT '',
  `branch_id` varchar(125) DEFAULT NULL,
  `sales_person` varchar(250) DEFAULT '',
  `emp_code` varchar(225) DEFAULT NULL,
  `model_name` varchar(225) DEFAULT NULL,
  `item_no` varchar(125) DEFAULT NULL,
  `product_name` varchar(250) DEFAULT '',
  `group_code` varchar(250) DEFAULT NULL,
  `itm_group_name` varchar(250) DEFAULT NULL,
  `tax_code` varchar(250) DEFAULT NULL,
  `quantity` bigint(20) DEFAULT 0,
  `rate` decimal(19,2) DEFAULT 0.00,
  `lp` decimal(19,2) DEFAULT NULL,
  `net_amount` decimal(19,2) DEFAULT 0.00,
  `tax_amount` decimal(19,2) DEFAULT 0.00,
  `cgst_amount` decimal(19,2) DEFAULT 0.00,
  `sgst_amount` decimal(19,2) DEFAULT 0.00,
  `igst_amount` decimal(19,2) DEFAULT 0.00,
  `sinv_gst_amt` decimal(19,2) DEFAULT 0.00,
  `total_amount` decimal(19,2) DEFAULT 0.00,
  `store_name` varchar(250) DEFAULT '',
  `group_name` varchar(250) DEFAULT '',
  `new_group` varchar(255) DEFAULT NULL,
  `branch` varchar(250) DEFAULT '',
  `new_group_name` varchar(250) DEFAULT '',
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `group_1` varchar(225) DEFAULT NULL,
  `group_2` varchar(225) DEFAULT NULL,
  `group_3` varchar(225) DEFAULT NULL,
  `group_4` varchar(225) DEFAULT NULL,
  `sap_code` varchar(225) DEFAULT NULL,
  `new_product` varchar(225) DEFAULT NULL,
  `new_dealer` varchar(225) DEFAULT NULL,
  `bp_code` varchar(225) DEFAULT NULL,
  `document_status` varchar(225) DEFAULT NULL,
  `canceled` varchar(225) DEFAULT NULL,
  `remarks` varchar(225) DEFAULT NULL,
  `serial_no` text DEFAULT NULL,
  `sell_from` text DEFAULT NULL,
  `delete_this` tinyint(4) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `primary_schemes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `scheme_name` varchar(250) NOT NULL,
  `scheme_description` longtext DEFAULT NULL,
  `start_date` date DEFAULT NULL,
  `end_date` date DEFAULT NULL,
  `repetition` int(11) DEFAULT NULL,
  `day_repeat` longtext DEFAULT NULL,
  `week_repeat` longtext DEFAULT NULL,
  `quarter` int(11) DEFAULT NULL,
  `customer_type` varchar(255) DEFAULT NULL,
  `scheme_type` varchar(200) DEFAULT NULL,
  `scheme_basedon` varchar(200) DEFAULT NULL,
  `per_pcs` int(11) DEFAULT 0,
  `assign_to` varchar(200) DEFAULT NULL,
  `branch` varchar(255) DEFAULT NULL,
  `state` varchar(255) DEFAULT NULL,
  `customer` varchar(255) DEFAULT NULL,
  `division` varchar(255) DEFAULT NULL,
  `minimum` bigint(20) DEFAULT NULL,
  `maximum` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `primary_schemes_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `primary_scheme_id` bigint(20) UNSIGNED NOT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `sap_code` varchar(225) DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `subcategory_id` bigint(20) UNSIGNED DEFAULT NULL,
  `group_type` varchar(125) DEFAULT NULL,
  `groups` varchar(125) DEFAULT NULL,
  `min` bigint(20) DEFAULT NULL,
  `max` bigint(20) DEFAULT NULL,
  `slab_min` decimal(19,2) DEFAULT NULL,
  `slab_max` decimal(19,2) DEFAULT NULL,
  `gift` varchar(225) DEFAULT NULL,
  `points` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `products` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `product_name` varchar(250) NOT NULL,
  `product_code` varchar(125) DEFAULT NULL,
  `new_group` varchar(125) DEFAULT NULL,
  `sub_group` varchar(125) DEFAULT NULL,
  `display_name` varchar(250) NOT NULL DEFAULT '',
  `description` varchar(450) NOT NULL DEFAULT '',
  `subcategory_id` bigint(20) UNSIGNED DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `brand_id` bigint(20) UNSIGNED DEFAULT NULL,
  `branch_id` varchar(125) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci DEFAULT NULL,
  `product_image` varchar(300) NOT NULL DEFAULT '',
  `expiry_interval` varchar(125) DEFAULT NULL,
  `expiry_interval_preiod` int(11) NOT NULL DEFAULT 0,
  `unit_id` bigint(20) UNSIGNED DEFAULT NULL,
  `hsn_sac_no` varchar(255) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `specification` varchar(255) NOT NULL DEFAULT '',
  `phase` text DEFAULT NULL,
  `part_no` varchar(250) NOT NULL DEFAULT '',
  `product_no` varchar(250) NOT NULL DEFAULT '',
  `model_no` varchar(250) NOT NULL DEFAULT '',
  `suc_del` varchar(191) DEFAULT NULL,
  `sap_code` varchar(225) DEFAULT NULL,
  `hsn_sac` bigint(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `product_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `detail_title` varchar(200) NOT NULL DEFAULT '',
  `detail_description` varchar(450) NOT NULL DEFAULT '',
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `detail_image` varchar(400) NOT NULL DEFAULT '',
  `mrp` decimal(8,2) DEFAULT NULL,
  `price` decimal(8,2) DEFAULT NULL,
  `discount` decimal(8,2) DEFAULT NULL COMMENT 'in percent',
  `max_discount` decimal(8,2) DEFAULT NULL COMMENT 'in percent',
  `rmc` decimal(8,2) DEFAULT 0.00,
  `selling_price` decimal(8,2) DEFAULT NULL,
  `gst` decimal(8,2) DEFAULT NULL COMMENT 'gst in percent',
  `isprimary` tinyint(4) NOT NULL DEFAULT 0,
  `stock_qty` bigint(20) DEFAULT 0,
  `hsn_code` varchar(250) DEFAULT NULL,
  `budget_for_month` varchar(250) DEFAULT NULL,
  `top_sku` varchar(250) DEFAULT NULL,
  `ean_code` varchar(250) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `redemptions` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) NOT NULL,
  `redeem_mode` varchar(255) NOT NULL,
  `account_holder` varchar(255) DEFAULT NULL,
  `account_number` varchar(255) DEFAULT NULL,
  `bank_name` varchar(255) DEFAULT NULL,
  `ifsc_code` varchar(255) DEFAULT NULL,
  `redeem_amount` varchar(255) DEFAULT NULL,
  `gift_id` bigint(20) DEFAULT NULL,
  `product_send` varchar(225) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `dispatch_number` varchar(125) DEFAULT NULL,
  `remark` varchar(125) DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `approve_date` date DEFAULT NULL,
  `dispatch_date` date DEFAULT NULL,
  `gift_recived_date` date DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deatils` varchar(225) DEFAULT NULL,
  `invoice_number` varchar(225) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `regions` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `region_name` varchar(250) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `resignations` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `submit_date` date DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `branch_id` bigint(20) DEFAULT NULL,
  `user_id` bigint(20) DEFAULT NULL,
  `employee_code` varchar(125) DEFAULT NULL,
  `notice` int(11) DEFAULT NULL,
  `date_of_joining` date DEFAULT NULL,
  `last_working_date` date DEFAULT NULL,
  `cug_sim_no` varchar(255) DEFAULT NULL,
  `reason` text DEFAULT NULL,
  `persoanla_email` varchar(255) DEFAULT NULL,
  `persoanla_mobile` varchar(255) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `remark` varchar(225) DEFAULT NULL,
  `status` int(11) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `resignation_check_lists` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `resignation_id` bigint(20) NOT NULL,
  `document_file` varchar(255) DEFAULT NULL,
  `exit_interview` varchar(255) DEFAULT NULL,
  `advance` varchar(255) DEFAULT NULL,
  `laptop` varchar(255) DEFAULT NULL,
  `sim_card` varchar(255) DEFAULT NULL,
  `keys` varchar(255) DEFAULT NULL,
  `visiting_card` varchar(255) DEFAULT NULL,
  `income_tax` varchar(255) DEFAULT NULL,
  `laptop_bag` varchar(255) DEFAULT NULL,
  `expense_voucher` varchar(255) DEFAULT NULL,
  `crm_id` varchar(255) DEFAULT NULL,
  `unpaid_salary` varchar(255) DEFAULT NULL,
  `data_email` varchar(255) DEFAULT NULL,
  `id_card` varchar(255) DEFAULT NULL,
  `payable_expense` varchar(255) DEFAULT NULL,
  `pen_drive` varchar(255) DEFAULT NULL,
  `bouns` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `sales` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `buyer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `seller_id` bigint(20) UNSIGNED DEFAULT NULL,
  `order_id` bigint(20) UNSIGNED DEFAULT NULL,
  `total_qty` bigint(20) NOT NULL DEFAULT 0,
  `shipped_qty` bigint(20) NOT NULL DEFAULT 0,
  `orderno` varchar(250) NOT NULL DEFAULT '',
  `fiscal_year` varchar(50) NOT NULL DEFAULT '',
  `sales_no` varchar(250) NOT NULL DEFAULT '',
  `invoice_no` varchar(250) NOT NULL DEFAULT '',
  `invoice_date` date DEFAULT NULL,
  `transport_details` text DEFAULT NULL,
  `total_gst` decimal(19,2) NOT NULL DEFAULT 0.00,
  `total_discount` decimal(19,2) DEFAULT NULL,
  `extra_discount` decimal(8,2) DEFAULT NULL,
  `extra_discount_amount` decimal(19,2) DEFAULT NULL,
  `sub_total` decimal(19,2) NOT NULL DEFAULT 0.00,
  `grand_total` decimal(19,2) NOT NULL DEFAULT 0.00,
  `paid_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `description` varchar(400) NOT NULL DEFAULT '',
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `transport_name` varchar(200) DEFAULT NULL,
  `lr_no` varchar(125) DEFAULT NULL,
  `dispatch_date` date DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `salestargetcustomers` (
  `id` int(11) NOT NULL,
  `customer_id` int(11) NOT NULL,
  `branch_id` varchar(125) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `div_id` varchar(125) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `month` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `year` year(4) DEFAULT NULL,
  `target` decimal(10,2) DEFAULT NULL,
  `achievement` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `achievement_percent` decimal(10,0) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `salestargetusers` (
  `id` bigint(20) NOT NULL,
  `user_id` bigint(20) NOT NULL,
  `branch_id` int(11) DEFAULT NULL,
  `month` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `year` year(4) DEFAULT NULL,
  `target` decimal(10,2) DEFAULT NULL,
  `achievement` decimal(10,2) DEFAULT NULL,
  `type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `achievement_percent` decimal(10,2) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `qunatity_target` decimal(10,2) DEFAULT NULL,
  `qunatity_achievement` decimal(10,2) DEFAULT NULL,
  `qunatity_achievement_percent` decimal(10,2) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `sales_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `sales_id` bigint(20) UNSIGNED DEFAULT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `product_detail_id` bigint(20) UNSIGNED DEFAULT NULL,
  `quantity` bigint(20) NOT NULL DEFAULT 0,
  `shipped_qty` bigint(20) NOT NULL DEFAULT 0,
  `price` decimal(19,2) NOT NULL DEFAULT 0.00,
  `discount` decimal(19,2) DEFAULT NULL,
  `discount_amount` decimal(19,2) DEFAULT NULL,
  `tax_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `line_total` decimal(19,2) NOT NULL DEFAULT 0.00,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `sales_targets` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `userid` bigint(20) UNSIGNED DEFAULT NULL,
  `startdate` datetime DEFAULT NULL,
  `enddate` datetime DEFAULT NULL,
  `amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `achievement` decimal(19,2) NOT NULL DEFAULT 0.00,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `sales_weightages` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` text NOT NULL,
  `weightage` varchar(191) DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `department_id` bigint(20) DEFAULT NULL,
  `designation_id` varchar(191) DEFAULT NULL,
  `category_name` varchar(191) DEFAULT NULL,
  `indicator` longtext DEFAULT NULL,
  `annum_target` varchar(191) DEFAULT NULL,
  `display_name` varchar(191) DEFAULT NULL,
  `financial_year` varchar(191) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `sap_stocks` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `product_sap_code` varchar(255) DEFAULT NULL,
  `product_description` varchar(255) DEFAULT NULL,
  `product_category_sap_code` varchar(255) DEFAULT NULL,
  `product_category_name` varchar(255) DEFAULT NULL,
  `warehouse_code` varchar(255) DEFAULT NULL,
  `warehouse_name` varchar(255) DEFAULT NULL,
  `instock_qty` varchar(255) DEFAULT NULL,
  `value` varchar(255) DEFAULT NULL,
  `itm_remarks` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `scheme_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `scheme_id` bigint(20) UNSIGNED NOT NULL,
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `subcategory_id` bigint(20) UNSIGNED DEFAULT NULL,
  `active_point` bigint(20) DEFAULT NULL,
  `provision_point` bigint(20) DEFAULT NULL,
  `points` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `scheme_headers` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `scheme_name` varchar(250) NOT NULL,
  `scheme_description` varchar(450) NOT NULL DEFAULT '',
  `start_date` date NOT NULL,
  `end_date` date NOT NULL,
  `scheme_basedon` varchar(125) DEFAULT NULL,
  `assign_to` varchar(125) DEFAULT NULL,
  `branch` varchar(125) DEFAULT NULL,
  `state` varchar(125) DEFAULT NULL,
  `customer` varchar(125) DEFAULT NULL,
  `customer_type` int(11) DEFAULT NULL,
  `points_start_date` date DEFAULT NULL,
  `points_end_date` date DEFAULT NULL,
  `block_points` bigint(20) NOT NULL DEFAULT 0,
  `block_percents` bigint(20) NOT NULL DEFAULT 0,
  `scheme_image` varchar(450) NOT NULL DEFAULT '',
  `scheme_type` varchar(200) NOT NULL DEFAULT '',
  `point_value` decimal(8,2) NOT NULL DEFAULT 0.00,
  `regions` varchar(450) NOT NULL DEFAULT '',
  `redeem_percents` tinyint(4) NOT NULL DEFAULT 0,
  `schemes` varchar(450) NOT NULL DEFAULT '',
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `secondary_customers` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `type` varchar(255) NOT NULL,
  `sub_type` varchar(255) DEFAULT NULL,
  `owner_name` varchar(255) NOT NULL,
  `shop_name` varchar(255) NOT NULL,
  `mobile_number` varchar(255) NOT NULL,
  `whatsapp_number` varchar(255) DEFAULT NULL,
  `owner_photo` varchar(255) DEFAULT NULL,
  `shop_photo` varchar(255) DEFAULT NULL,
  `vehicle_segment` varchar(255) DEFAULT NULL,
  `address_line` text NOT NULL,
  `belt_area_market_name` varchar(255) DEFAULT NULL,
  `saathi_awareness_status` enum('Done','Not Done') NOT NULL DEFAULT 'Not Done',
  `nistha_awareness_status` enum('Done','Not Done') DEFAULT NULL,
  `opportunity_status` enum('HOT','WARM','COLD','LOST') NOT NULL DEFAULT 'COLD',
  `status` enum('PENDING','APPROVED','REJECTED') DEFAULT 'PENDING',
  `active` enum('Y','N') DEFAULT 'Y',
  `gps_location` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `country_id` bigint(20) UNSIGNED NOT NULL,
  `state_id` bigint(20) UNSIGNED NOT NULL,
  `district_id` bigint(20) UNSIGNED NOT NULL,
  `city_id` bigint(20) UNSIGNED NOT NULL,
  `pincode_id` bigint(20) UNSIGNED NOT NULL,
  `beat_id` bigint(20) UNSIGNED DEFAULT NULL,
  `country` varchar(255) DEFAULT NULL,
  `state` varchar(255) DEFAULT NULL,
  `district` varchar(255) DEFAULT NULL,
  `city` varchar(255) DEFAULT NULL,
  `pincode` varchar(255) DEFAULT NULL,
  `saathi_awareness` tinyint(4) NOT NULL DEFAULT 1,
  `distributor_name` varchar(255) DEFAULT NULL,
  `agri_distributor` varchar(255) DEFAULT NULL,
  `gst_number` varchar(255) DEFAULT NULL,
  `pan_number` varchar(255) DEFAULT NULL,
  `gst_attachment` varchar(255) DEFAULT NULL,
  `pan_attachment` varchar(255) DEFAULT NULL,
  `bank_proof` varchar(255) DEFAULT NULL,
  `bank_account_type` varchar(255) DEFAULT NULL,
  `bank_account_number` varchar(255) DEFAULT NULL,
  `bank_name` varchar(255) DEFAULT NULL,
  `ifsc_code` varchar(255) DEFAULT NULL,
  `account_holder_name` varchar(255) DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `employee_id` varchar(255) DEFAULT NULL,
  `remark` varchar(255) NOT NULL,
  `approve_reject_by` bigint(20) DEFAULT NULL,
  `gmap` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `services` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `product_code` varchar(255) DEFAULT NULL,
  `invoice_no` varchar(255) DEFAULT NULL,
  `invoice_date` date DEFAULT NULL,
  `branch_code` varchar(255) DEFAULT NULL,
  `party_name` varchar(255) DEFAULT NULL,
  `customer_id` varchar(255) DEFAULT NULL,
  `bp_code` varchar(255) DEFAULT NULL,
  `product_name` varchar(255) DEFAULT NULL,
  `product_description` text DEFAULT NULL,
  `product_store` varchar(125) DEFAULT NULL,
  `qty` varchar(255) DEFAULT NULL,
  `group` varchar(255) DEFAULT NULL,
  `new_group` varchar(255) DEFAULT NULL,
  `serial_no` varchar(255) DEFAULT NULL,
  `narration` varchar(255) DEFAULT NULL,
  `created_by` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_bills` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `bill_no` varchar(255) DEFAULT NULL,
  `complaint_id` varchar(255) DEFAULT NULL,
  `complaint_no` varchar(255) DEFAULT NULL,
  `division` varchar(255) DEFAULT NULL,
  `category` varchar(255) DEFAULT NULL,
  `complaint_type` varchar(255) DEFAULT NULL,
  `complaint_reason` varchar(255) DEFAULT NULL,
  `condition_of_service` varchar(255) DEFAULT NULL,
  `received_product` varchar(255) DEFAULT NULL,
  `nature_of_fault` varchar(255) DEFAULT NULL,
  `service_location` varchar(255) DEFAULT NULL,
  `repaired_replacement` varchar(255) DEFAULT NULL,
  `replacement_tag` varchar(255) DEFAULT NULL,
  `replacement_tag_number` varchar(255) DEFAULT NULL,
  `line_voltage` varchar(255) DEFAULT NULL,
  `load_voltage` varchar(255) DEFAULT NULL,
  `current` varchar(255) DEFAULT NULL,
  `water_source` varchar(255) DEFAULT NULL,
  `panel_rating_running` varchar(255) DEFAULT NULL,
  `panel_rating_starting` varchar(255) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_bill_complaint_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `service_bill_complaint_type_name` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_bill_product_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `service_bill_id` bigint(20) DEFAULT NULL,
  `service_type` varchar(255) DEFAULT NULL,
  `product_id` varchar(255) DEFAULT NULL,
  `quantity` varchar(255) DEFAULT NULL,
  `distance` varchar(255) DEFAULT NULL,
  `appreciation` varchar(255) DEFAULT NULL,
  `price` varchar(255) DEFAULT NULL,
  `subtotal` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_charge_categories` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `category_name` varchar(250) NOT NULL,
  `subcategory_image` varchar(350) NOT NULL DEFAULT '',
  `division_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_charge_charge_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(255) NOT NULL,
  `charge_type` varchar(255) DEFAULT NULL,
  `created_by` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_charge_divisions` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(255) NOT NULL,
  `division_name` varchar(255) DEFAULT NULL,
  `created_by` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_charge_products` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(125) NOT NULL DEFAULT 'Y',
  `charge_type_id` bigint(20) DEFAULT NULL,
  `product_name` varchar(255) DEFAULT NULL,
  `division_id` bigint(20) DEFAULT NULL,
  `category_id` bigint(20) DEFAULT NULL,
  `price` varchar(255) DEFAULT NULL,
  `other_charge` varchar(125) DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_complaint_reasons` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `service_bill_complaint_id` bigint(20) UNSIGNED NOT NULL,
  `service_complaint_reasons` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `service_group_complaints` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `subcategory_id` bigint(20) UNSIGNED NOT NULL,
  `service_bill_complaint_id` bigint(20) UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `settings` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `title` varchar(250) NOT NULL DEFAULT '',
  `key_name` varchar(250) NOT NULL DEFAULT '',
  `value` varchar(250) NOT NULL DEFAULT '',
  `module` varchar(200) NOT NULL DEFAULT '',
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `shipping_addresses` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `address1` varchar(250) NOT NULL DEFAULT '',
  `address2` varchar(250) NOT NULL DEFAULT '',
  `landmark` varchar(250) NOT NULL DEFAULT '',
  `locality` varchar(250) NOT NULL DEFAULT '',
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `country_id` bigint(20) UNSIGNED DEFAULT NULL,
  `state_id` bigint(20) UNSIGNED DEFAULT NULL,
  `district_id` bigint(20) UNSIGNED DEFAULT NULL,
  `city_id` bigint(20) UNSIGNED DEFAULT NULL,
  `pincode_id` bigint(20) UNSIGNED DEFAULT NULL,
  `zipcode` varchar(250) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `model_type` varchar(255) DEFAULT NULL,
  `model_id` varchar(255) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `states` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `state_name` varchar(250) NOT NULL,
  `country_id` bigint(20) UNSIGNED DEFAULT NULL,
  `gst_code` varchar(255) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `statuses` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `status_name` varchar(200) NOT NULL,
  `display_name` varchar(250) NOT NULL DEFAULT '',
  `status_message` varchar(400) NOT NULL,
  `module` varchar(250) NOT NULL DEFAULT '',
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `subcategories` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `ranking` int(11) NOT NULL DEFAULT 1,
  `subcategory_name` varchar(250) NOT NULL,
  `subcategory_image` varchar(350) NOT NULL DEFAULT '',
  `sap_code` varchar(350) DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `service_category_id` varchar(191) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `supports` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `subject` varchar(200) NOT NULL DEFAULT '',
  `description` varchar(450) NOT NULL DEFAULT '',
  `full_name` varchar(450) NOT NULL DEFAULT '',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `assigned_to` bigint(20) UNSIGNED DEFAULT NULL,
  `isoverdue` int(11) NOT NULL DEFAULT 0,
  `reopened` int(11) NOT NULL DEFAULT 0,
  `isanswered` int(11) NOT NULL DEFAULT 0,
  `is_transferred` tinyint(4) NOT NULL DEFAULT 0,
  `assigned_at` datetime DEFAULT NULL,
  `transferred_at` datetime DEFAULT NULL,
  `reopened_at` datetime DEFAULT NULL,
  `duedate` datetime DEFAULT NULL,
  `closed_at` datetime DEFAULT NULL,
  `last_message_at` datetime DEFAULT NULL,
  `lock_at` datetime DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `survey_data` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `field_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `value` varchar(400) DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `tasks` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `title` varchar(300) NOT NULL DEFAULT '',
  `descriptions` varchar(255) NOT NULL DEFAULT '',
  `task_department_id` bigint(20) DEFAULT NULL,
  `task_type` varchar(50) DEFAULT NULL,
  `task_project_id` bigint(50) DEFAULT NULL,
  `task_priority_id` bigint(50) DEFAULT NULL,
  `lead_id` bigint(50) DEFAULT NULL,
  `due_datetime` datetime DEFAULT NULL,
  `datetime` datetime DEFAULT NULL,
  `reminder` datetime DEFAULT NULL,
  `open_datetime` datetime DEFAULT NULL,
  `inprogress_datetime` datetime DEFAULT NULL,
  `reopen_datetime` datetime DEFAULT NULL,
  `completed_at` datetime DEFAULT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `is_done` tinyint(1) NOT NULL DEFAULT 0,
  `remark` varchar(1000) NOT NULL DEFAULT '',
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `task_status` enum('Pending','Open','In progress','Completed','Reopen') NOT NULL DEFAULT 'Pending',
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `task_assignments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `task_id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `task_comments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `task_id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `comment` text NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `task_departments` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `task_priorities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `task_projects` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `task_status_logs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `task_id` bigint(20) UNSIGNED DEFAULT NULL,
  `previous_status` varchar(50) DEFAULT NULL,
  `new_status` varchar(50) DEFAULT NULL,
  `changed_by` bigint(20) UNSIGNED DEFAULT NULL,
  `comments` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `tax_invoice_tax` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `tax_name` varchar(255) NOT NULL,
  `tax_percentage` decimal(5,2) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `tax_invoice_tds` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `tax_name` varchar(255) NOT NULL,
  `rate` decimal(5,2) NOT NULL,
  `section` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `tour_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `tourid` bigint(20) UNSIGNED DEFAULT NULL,
  `city_id` bigint(20) UNSIGNED DEFAULT NULL,
  `visited_date` date DEFAULT NULL,
  `visited_cityid` bigint(20) UNSIGNED DEFAULT NULL,
  `last_visited` date DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `tour_programmes` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `date` date DEFAULT NULL,
  `userid` bigint(20) UNSIGNED DEFAULT NULL,
  `town` varchar(250) NOT NULL DEFAULT '',
  `district` bigint(20) NOT NULL,
  `objectives` varchar(255) NOT NULL DEFAULT '',
  `type` varchar(50) NOT NULL DEFAULT '',
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `transaction_histories` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `customer_id` bigint(20) NOT NULL,
  `coupon_code` varchar(255) DEFAULT NULL,
  `active_point` bigint(20) DEFAULT NULL,
  `provision_point` bigint(20) DEFAULT NULL,
  `point` int(11) DEFAULT NULL,
  `scheme_id` bigint(20) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `remark` text DEFAULT NULL,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `unit_measures` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `unit_name` varchar(250) NOT NULL,
  `unit_code` varchar(250) NOT NULL DEFAULT '',
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `updated_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `user_activities` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `userid` bigint(20) UNSIGNED NOT NULL,
  `customerid` bigint(20) UNSIGNED DEFAULT NULL,
  `latitude` varchar(50) DEFAULT NULL,
  `longitude` varchar(50) DEFAULT NULL,
  `time` datetime DEFAULT NULL,
  `address` varchar(450) NOT NULL DEFAULT '',
  `description` varchar(450) NOT NULL DEFAULT '',
  `type` varchar(50) NOT NULL DEFAULT '',
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `user_daily_lat_longs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `date` date DEFAULT NULL,
  `latitude` varchar(50) DEFAULT NULL,
  `longitude` varchar(50) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `user_live_locations` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `userid` bigint(20) UNSIGNED NOT NULL,
  `latitude` varchar(50) DEFAULT NULL,
  `longitude` varchar(50) DEFAULT NULL,
  `time` datetime DEFAULT NULL,
  `address` varchar(450) DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `user_logins` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `entry_from` varchar(250) NOT NULL DEFAULT '',
  `provider` varchar(250) NOT NULL DEFAULT '',
  `mobile` varchar(250) NOT NULL DEFAULT '',
  `login_at` datetime DEFAULT NULL,
  `logout_at` datetime DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `user_pms_remarks` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `fyear` varchar(255) DEFAULT NULL,
  `recommended_increment` int(11) DEFAULT NULL,
  `recommended_designation` bigint(20) UNSIGNED DEFAULT NULL,
  `remark` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `visitors` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `ip_address` varchar(255) NOT NULL,
  `country` varchar(255) DEFAULT NULL,
  `state` varchar(255) DEFAULT NULL,
  `city` varchar(125) DEFAULT NULL,
  `system_name` varchar(255) NOT NULL,
  `device` varchar(255) DEFAULT NULL,
  `browser` varchar(255) DEFAULT NULL,
  `is_mobile` tinyint(1) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `visit_reports` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `checkin_id` bigint(20) UNSIGNED DEFAULT NULL,
  `user_id` bigint(20) UNSIGNED DEFAULT NULL,
  `customer_id` bigint(20) UNSIGNED DEFAULT NULL,
  `visit_type_id` bigint(20) UNSIGNED DEFAULT NULL,
  `report_title` varchar(200) NOT NULL DEFAULT '',
  `description` varchar(450) NOT NULL DEFAULT '',
  `visit_image` varchar(450) NOT NULL DEFAULT '',
  `next_visit` datetime DEFAULT NULL,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `visit_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `type_name` varchar(250) NOT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `wallets` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `customer_id` bigint(20) UNSIGNED NOT NULL,
  `scheme_id` bigint(20) UNSIGNED DEFAULT NULL,
  `schemedetail_id` bigint(20) UNSIGNED DEFAULT NULL,
  `points` bigint(20) NOT NULL DEFAULT 0,
  `point_type` varchar(20) NOT NULL DEFAULT '',
  `invoice_amount` decimal(19,2) NOT NULL DEFAULT 0.00,
  `invoice_no` varchar(200) NOT NULL DEFAULT '',
  `coupon_code` varchar(250) NOT NULL DEFAULT '',
  `invoice_date` date DEFAULT NULL,
  `transaction_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `transaction_type` varchar(20) NOT NULL DEFAULT '',
  `sales_id` bigint(20) UNSIGNED DEFAULT NULL,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `checkinid` bigint(20) UNSIGNED DEFAULT NULL,
  `quantity` bigint(20) NOT NULL DEFAULT 0,
  `userid` bigint(20) UNSIGNED DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `wallet_details` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `active` varchar(1) NOT NULL DEFAULT 'Y',
  `wallet_id` bigint(20) UNSIGNED NOT NULL,
  `points` bigint(20) NOT NULL DEFAULT 0,
  `coupon_code` varchar(250) NOT NULL DEFAULT '',
  `product_id` bigint(20) UNSIGNED DEFAULT NULL,
  `category_id` bigint(20) UNSIGNED DEFAULT NULL,
  `subcategory_id` bigint(20) UNSIGNED DEFAULT NULL,
  `quantity` bigint(20) NOT NULL DEFAULT 0,
  `deleted_at` timestamp NULL DEFAULT NULL,
  `status_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `ware_houses` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `warehouse_code` varchar(255) DEFAULT NULL,
  `warehouse_name` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `warranty_activations` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `product_serail_number` varchar(255) DEFAULT NULL,
  `product_id` bigint(20) DEFAULT NULL,
  `branch_id` bigint(20) DEFAULT NULL,
  `end_user_id` bigint(20) DEFAULT NULL,
  `customer_id` varchar(255) DEFAULT NULL,
  `sale_bill_no` varchar(255) DEFAULT NULL,
  `sale_bill_date` date DEFAULT NULL,
  `warranty_date` date DEFAULT NULL,
  `remark` varchar(125) DEFAULT NULL,
  `status` tinyint(4) NOT NULL DEFAULT 0,
  `created_by` bigint(20) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS `warranty_timelines` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `warranty_id` bigint(20) UNSIGNED DEFAULT NULL,
  `created_by` bigint(20) UNSIGNED DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `remark` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
            migrationBuilder.Sql(@"ALTER TABLE `active_customer_processes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `active_customer_processes_customer_id_foreign` (`customer_id`),
  ADD KEY `active_customer_processes_process_id_foreign` (`process_id`),
  ADD KEY `active_customer_processes_assigned_by_foreign` (`assigned_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `active_customer_process_steps`
  ADD PRIMARY KEY (`id`),
  ADD KEY `active_customer_process_steps_active_customer_process_id_foreign` (`active_customer_process_id`),
  ADD KEY `active_customer_process_steps_customer_process_step_id_foreign` (`customer_process_step_id`),
  ADD KEY `active_customer_process_steps_completed_by_foreign` (`completed_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `addresses`
  ADD PRIMARY KEY (`id`),
  ADD KEY `addresses_address1_index` (`address1`),
  ADD KEY `addresses_address2_index` (`address2`),
  ADD KEY `addresses_landmark_index` (`landmark`),
  ADD KEY `addresses_locality_index` (`locality`),
  ADD KEY `addresses_customer_id_index` (`customer_id`),
  ADD KEY `addresses_user_id_index` (`user_id`),
  ADD KEY `addresses_country_id_index` (`country_id`),
  ADD KEY `addresses_state_id_index` (`state_id`),
  ADD KEY `addresses_district_id_index` (`district_id`),
  ADD KEY `addresses_city_id_index` (`city_id`),
  ADD KEY `addresses_pincode_id_index` (`pincode_id`),
  ADD KEY `addresses_zipcode_index` (`zipcode`);");
            migrationBuilder.Sql(@"ALTER TABLE `appraisals`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `attachments`
  ADD PRIMARY KEY (`id`),
  ADD KEY `attachments_product_id_index` (`product_id`),
  ADD KEY `attachments_user_id_index` (`user_id`),
  ADD KEY `attachments_customer_id_index` (`customer_id`),
  ADD KEY `attachments_order_id_index` (`order_id`),
  ADD KEY `attachments_sales_id_index` (`sales_id`),
  ADD KEY `attachments_file_path_index` (`file_path`),
  ADD KEY `attachments_document_name_index` (`document_name`);");
            migrationBuilder.Sql(@"ALTER TABLE `attendances`
  ADD PRIMARY KEY (`id`),
  ADD KEY `attendances_user_id_index` (`user_id`),
  ADD KEY `attendances_punchin_date_index` (`punchin_date`),
  ADD KEY `attendances_punchin_time_index` (`punchin_time`),
  ADD KEY `attendances_punchin_longitude_index` (`punchin_longitude`),
  ADD KEY `attendances_punchin_latitude_index` (`punchin_latitude`),
  ADD KEY `attendances_punchin_address_index` (`punchin_address`),
  ADD KEY `attendances_punchin_image_index` (`punchin_image`),
  ADD KEY `attendances_punchout_date_index` (`punchout_date`),
  ADD KEY `attendances_punchout_time_index` (`punchout_time`),
  ADD KEY `attendances_punchout_latitude_index` (`punchout_latitude`),
  ADD KEY `attendances_punchout_longitude_index` (`punchout_longitude`),
  ADD KEY `attendances_punchout_address_index` (`punchout_address`),
  ADD KEY `attendances_punchout_image_index` (`punchout_image`),
  ADD KEY `attendances_punchin_summary_index` (`punchin_summary`),
  ADD KEY `attendances_punchout_summary_index` (`punchout_summary`),
  ADD KEY `attendances_worked_time_index` (`worked_time`),
  ADD KEY `attendances_working_type_index` (`working_type`);");
            migrationBuilder.Sql(@"ALTER TABLE `beats`
  ADD PRIMARY KEY (`id`),
  ADD KEY `beats_beat_name_index` (`beat_name`),
  ADD KEY `beats_description_index` (`description`),
  ADD KEY `beats_region_id_index` (`region_id`),
  ADD KEY `beats_country_id_index` (`country_id`),
  ADD KEY `beats_state_id_index` (`state_id`),
  ADD KEY `beats_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `beat_customers`
  ADD PRIMARY KEY (`id`),
  ADD KEY `beat_customers_beat_id_index` (`beat_id`),
  ADD KEY `beat_customers_customer_id_index` (`customer_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `beat_schedules`
  ADD PRIMARY KEY (`id`),
  ADD KEY `beat_schedules_beat_id_index` (`beat_id`),
  ADD KEY `beat_schedules_beat_date_index` (`beat_date`),
  ADD KEY `beat_schedules_user_id_index` (`user_id`),
  ADD KEY `beat_schedules_tourid_index` (`tourid`);");
            migrationBuilder.Sql(@"ALTER TABLE `beat_users`
  ADD PRIMARY KEY (`id`),
  ADD KEY `beat_users_beat_id_index` (`beat_id`),
  ADD KEY `beat_users_user_id_index` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `branches`
  ADD PRIMARY KEY (`id`),
  ADD KEY `branches_branch_name_index` (`branch_name`),
  ADD KEY `branches_created_by_index` (`created_by`),
  ADD KEY `branches_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `branchwise_targets`
  ADD PRIMARY KEY (`id`),
  ADD KEY `branchwise_targets_user_id_index` (`user_id`),
  ADD KEY `branchwise_targets_branch_id_index` (`branch_id`),
  ADD KEY `branchwise_targets_div_id_index` (`div_id`),
  ADD KEY `branchwise_targets_target_index` (`target`),
  ADD KEY `branchwise_targets_achievement_index` (`achievement`),
  ADD KEY `branchwise_targets_amount_index` (`amount`);");
            migrationBuilder.Sql(@"ALTER TABLE `branch_oprning_quantities`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `branch_stocks`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `brands`
  ADD PRIMARY KEY (`id`),
  ADD KEY `brands_brand_name_index` (`brand_name`),
  ADD KEY `brands_brand_image_index` (`brand_image`),
  ADD KEY `brands_created_by_index` (`created_by`),
  ADD KEY `brands_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `call_logs`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `categories`
  ADD PRIMARY KEY (`id`),
  ADD KEY `categories_ranking_index` (`ranking`),
  ADD KEY `categories_category_name_index` (`category_name`),
  ADD KEY `categories_category_image_index` (`category_image`),
  ADD KEY `categories_created_by_index` (`created_by`),
  ADD KEY `categories_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `check_in`
  ADD PRIMARY KEY (`id`),
  ADD KEY `check_in_user_id_index` (`user_id`),
  ADD KEY `check_in_checkin_date_index` (`checkin_date`),
  ADD KEY `check_in_checkin_time_index` (`checkin_time`),
  ADD KEY `check_in_checkin_latitude_index` (`checkin_latitude`),
  ADD KEY `check_in_checkin_longitude_index` (`checkin_longitude`),
  ADD KEY `check_in_checkin_address_index` (`checkin_address`),
  ADD KEY `check_in_checkout_date_index` (`checkout_date`),
  ADD KEY `check_in_checkout_time_index` (`checkout_time`),
  ADD KEY `check_in_checkout_latitude_index` (`checkout_latitude`),
  ADD KEY `check_in_checkout_longitude_index` (`checkout_longitude`),
  ADD KEY `check_in_checkout_address_index` (`checkout_address`),
  ADD KEY `check_in_distance_index` (`distance`),
  ADD KEY `check_in_beatscheduleid_index` (`beatscheduleid`);");
            migrationBuilder.Sql(@"ALTER TABLE `check_in_drafts`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `cities`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `cities_district_id_city_name_unique` (`district_id`,`city_name`),
  ADD KEY `cities_city_name_index` (`city_name`),
  ADD KEY `cities_district_id_index` (`district_id`),
  ADD KEY `cities_created_by_index` (`created_by`),
  ADD KEY `cities_updated_by_index` (`updated_by`),
  ADD KEY `cities_state_id_index` (`state_id`),
  ADD KEY `cities_grade_index` (`grade`);");
            migrationBuilder.Sql(@"ALTER TABLE `claim_generations`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `claim_generation_details`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `complaints`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `complaint_timelines`
  ADD PRIMARY KEY (`id`),
  ADD KEY `complaint_timelines_complaint_id_index` (`complaint_id`),
  ADD KEY `complaint_timelines_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `complaint_types`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `complaint_work_dones`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `comp_off_leaves`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `countries`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `countries_country_name_unique` (`country_name`),
  ADD KEY `countries_created_by_index` (`created_by`),
  ADD KEY `countries_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `coupons`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `coupons_coupon_unique` (`coupon`),
  ADD KEY `coupons_points_index` (`points`),
  ADD KEY `coupons_expiry_date_index` (`expiry_date`),
  ADD KEY `coupons_product_id_index` (`product_id`),
  ADD KEY `coupons_coupon_profile_id_index` (`coupon_profile_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `coupon_profiles`
  ADD PRIMARY KEY (`id`),
  ADD KEY `coupon_profiles_profile_name_index` (`profile_name`),
  ADD KEY `coupon_profiles_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_custom_fields`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `customer_custom_fields_field_key_unique` (`field_key`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_custom_field_values`
  ADD PRIMARY KEY (`id`),
  ADD KEY `customer_custom_field_values_custom_field_id_foreign` (`custom_field_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `customer_details_gstin_no_index` (`gstin_no`),
  ADD KEY `customer_details_pan_no_index` (`pan_no`),
  ADD KEY `customer_details_aadhar_no_index` (`aadhar_no`),
  ADD KEY `customer_details_otherid_no_index` (`otherid_no`),
  ADD KEY `customer_details_shop_image_index` (`shop_image`),
  ADD KEY `customer_details_visiting_card_index` (`visiting_card`),
  ADD KEY `customer_details_grade_index` (`grade`),
  ADD KEY `customer_details_visit_status_index` (`visit_status`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_outstantings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_processes`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_process_steps`
  ADD PRIMARY KEY (`id`),
  ADD KEY `customer_process_steps_customer_process_id_foreign` (`customer_process_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `customer_types`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `customer_types_customertype_name_unique` (`customertype_name`),
  ADD KEY `customer_types_type_name_index` (`type_name`),
  ADD KEY `customer_types_created_by_index` (`created_by`),
  ADD KEY `customer_types_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `custom_pdf_values`
  ADD PRIMARY KEY (`id`),
  ADD KEY `custom_pdf_values_estimate_id_index` (`estimate_id`),
  ADD KEY `custom_pdf_values_label_id_index` (`label_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `damage_entries`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `dealer_appointments`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `dealer_appointment_kycs`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `dealer_portal_settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `deal_ins`
  ADD PRIMARY KEY (`id`),
  ADD KEY `deal_ins_customer_id_index` (`customer_id`),
  ADD KEY `deal_ins_types_index` (`types`);");
            migrationBuilder.Sql(@"ALTER TABLE `departments`
  ADD PRIMARY KEY (`id`),
  ADD KEY `departments_department_name_index` (`name`),
  ADD KEY `departments_created_by_index` (`created_by`),
  ADD KEY `departments_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `designations`
  ADD PRIMARY KEY (`id`),
  ADD KEY `designations_designation_name_index` (`designation_name`),
  ADD KEY `designations_created_by_index` (`created_by`),
  ADD KEY `designations_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `districts`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `districts_state_id_district_name_unique` (`state_id`,`district_name`),
  ADD KEY `districts_district_name_index` (`district_name`),
  ADD KEY `districts_state_id_index` (`state_id`),
  ADD KEY `districts_created_by_index` (`created_by`),
  ADD KEY `districts_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `divisions`
  ADD PRIMARY KEY (`id`),
  ADD KEY `divisions_division_name_index` (`division_name`),
  ADD KEY `divisions_created_by_index` (`created_by`),
  ADD KEY `divisions_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `employee_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `index_customer_id` (`customer_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `end_users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `end_users_customer_number_unique` (`customer_number`);");
            migrationBuilder.Sql(@"ALTER TABLE `estimates`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `estimate_no` (`estimate_no`),
  ADD KEY `customer_id` (`customer_id`),
  ADD KEY `user_id` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `estimate_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `estimate_id` (`estimate_id`),
  ADD KEY `product_id` (`product_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `expenses`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `expenses_types`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `expense_logs`
  ADD PRIMARY KEY (`id`),
  ADD KEY `expense_logs_expense_id_index` (`expense_id`),
  ADD KEY `expense_logs_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `failed_jobs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `failed_jobs_uuid_unique` (`uuid`);");
            migrationBuilder.Sql(@"ALTER TABLE `fields`
  ADD PRIMARY KEY (`id`),
  ADD KEY `fields_field_name_index` (`field_name`),
  ADD KEY `fields_field_type_index` (`field_type`),
  ADD KEY `fields_label_name_index` (`label_name`),
  ADD KEY `fields_placeholder_index` (`placeholder`),
  ADD KEY `fields_module_index` (`module`),
  ADD KEY `fields_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `fieldsdata`
  ADD PRIMARY KEY (`id`),
  ADD KEY `fieldsdata_field_id_index` (`field_id`),
  ADD KEY `fieldsdata_value_index` (`value`);");
            migrationBuilder.Sql(@"ALTER TABLE `field_konnect_app_settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `firm_types`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `firm_types_firmtype_name_unique` (`firmtype_name`),
  ADD KEY `firm_types_created_by_index` (`created_by`),
  ADD KEY `firm_types_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `gamifications`
  ADD PRIMARY KEY (`id`),
  ADD KEY `gamifications_user_id_index` (`user_id`),
  ADD KEY `gamifications_customer_id_index` (`customer_id`),
  ADD KEY `gamifications_type_index` (`type`),
  ADD KEY `gamifications_points_index` (`points`);");
            migrationBuilder.Sql(@"ALTER TABLE `geo_locator_settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `gifts`
  ADD PRIMARY KEY (`id`),
  ADD KEY `gifts_product_name_index` (`product_name`),
  ADD KEY `gifts_display_name_index` (`display_name`),
  ADD KEY `gifts_description_index` (`description`),
  ADD KEY `gifts_product_image_index` (`product_image`),
  ADD KEY `gifts_mrp_index` (`mrp`),
  ADD KEY `gifts_price_index` (`price`),
  ADD KEY `gifts_points_index` (`points`),
  ADD KEY `gifts_subcategory_id_index` (`subcategory_id`),
  ADD KEY `gifts_category_id_index` (`category_id`),
  ADD KEY `gifts_brand_id_index` (`brand_id`),
  ADD KEY `gifts_unit_id_index` (`unit_id`),
  ADD KEY `gifts_created_by_index` (`created_by`),
  ADD KEY `gifts_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `giftsubcategories`
  ADD PRIMARY KEY (`id`),
  ADD KEY `giftsubcategories_subcategory_name_index` (`subcategory_name`),
  ADD KEY `giftsubcategories_subcategory_image_index` (`subcategory_image`),
  ADD KEY `giftsubcategories_category_id_index` (`category_id`),
  ADD KEY `giftsubcategories_created_by_index` (`created_by`),
  ADD KEY `giftsubcategories_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `gift_brands`
  ADD PRIMARY KEY (`id`),
  ADD KEY `gift_brands_ranking_index` (`ranking`),
  ADD KEY `gift_brands_brand_name_index` (`brand_name`),
  ADD KEY `gift_brands_brand_image_index` (`brand_image`),
  ADD KEY `gift_brands_created_by_index` (`created_by`),
  ADD KEY `gift_brands_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `gift_categories`
  ADD PRIMARY KEY (`id`),
  ADD KEY `gift_categories_ranking_index` (`ranking`),
  ADD KEY `gift_categories_category_name_index` (`category_name`),
  ADD KEY `gift_categories_category_image_index` (`category_image`),
  ADD KEY `gift_categories_created_by_index` (`created_by`),
  ADD KEY `gift_categories_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `gift_models`
  ADD PRIMARY KEY (`id`),
  ADD KEY `gift_models_model_name_index` (`model_name`),
  ADD KEY `gift_models_model_image_index` (`model_image`),
  ADD KEY `gift_models_sub_category_id_index` (`sub_category_id`),
  ADD KEY `gift_models_created_by_index` (`created_by`),
  ADD KEY `gift_models_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `gift_redemption_details`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `holidays`
  ADD PRIMARY KEY (`id`),
  ADD KEY `holidays_name_index` (`name2`),
  ADD KEY `holidays_branch_index` (`branch`),
  ADD KEY `holidays_created_by_index` (`created_by`),
  ADD KEY `holidays_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `invoices`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `invoice_no` (`invoice_no`),
  ADD KEY `customer_id` (`customer_id`),
  ADD KEY `user_id` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `invoice_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `invoice_id` (`invoice_id`),
  ADD KEY `product_id` (`product_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `invoice_labels`
  ADD PRIMARY KEY (`id`),
  ADD KEY `invoice_labels_invoice_setting_id_foreign` (`invoice_setting_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `invoice_settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `jobs`
  ADD PRIMARY KEY (`id`),
  ADD KEY `jobs_queue_index` (`queue`);");
            migrationBuilder.Sql(@"ALTER TABLE `leads`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_check_in`
  ADD PRIMARY KEY (`id`),
  ADD KEY `check_in_lead_id_foreign` (`lead_id`),
  ADD KEY `check_in_user_id_foreign` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_contacts`
  ADD PRIMARY KEY (`id`),
  ADD KEY `lead_contacts_lead_id_foreign` (`lead_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_logs`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_notes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `lead_notes_lead_id_foreign` (`lead_id`),
  ADD KEY `lead_notes_created_by_foreign` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_notifications`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_id` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_opportunities`
  ADD PRIMARY KEY (`id`),
  ADD KEY `lead_opportunities_lead_id_foreign` (`lead_id`),
  ADD KEY `lead_opportunities_assigned_to_foreign` (`assigned_to`),
  ADD KEY `lead_opportunities_lead_contact_id_foreign` (`lead_contact_id`),
  ADD KEY `lead_opportunities_created_by_foreign` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `lead_tasks`
  ADD PRIMARY KEY (`id`),
  ADD KEY `lead_tasks_lead_id_foreign` (`lead_id`),
  ADD KEY `lead_tasks_assigned_to_foreign` (`assigned_to`),
  ADD KEY `lead_tasks_created_by_foreign` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `leaves`
  ADD PRIMARY KEY (`id`),
  ADD KEY `leaves_user_id_index` (`user_id`),
  ADD KEY `leaves_from_date_index` (`from_date`),
  ADD KEY `leaves_to_date_index` (`to_date`),
  ADD KEY `leaves_type_index` (`type`),
  ADD KEY `leaves_reason_index` (`reason`),
  ADD KEY `leaves_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `loyalty_app_settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `marketings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `marketing_activities`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligences_fielddatas`
  ADD PRIMARY KEY (`id`),
  ADD KEY `market_intelligences_fielddatas_field_id_index` (`field_id`),
  ADD KEY `market_intelligences_fielddatas_value_index` (`value`);");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligences_fields`
  ADD PRIMARY KEY (`id`),
  ADD KEY `market_intelligences_fields_field_name_index` (`field_name`),
  ADD KEY `market_intelligences_fields_field_type_index` (`field_type`),
  ADD KEY `market_intelligences_fields_label_name_index` (`label_name`),
  ADD KEY `market_intelligences_fields_placeholder_index` (`placeholder`),
  ADD KEY `market_intelligences_fields_module_index` (`module`),
  ADD KEY `market_intelligences_fields_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligence_serveys`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligence_servey_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `market_intelligence_servey_data_servey_id_index` (`servey_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `master_distributors`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `master_distributors_distributor_code_unique` (`distributor_code`);");
            migrationBuilder.Sql(@"ALTER TABLE `media`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `media_uuid_unique` (`uuid`),
  ADD KEY `media_model_type_model_id_index` (`model_type`,`model_id`),
  ADD KEY `media_order_column_index` (`order_column`);");
            migrationBuilder.Sql(@"ALTER TABLE `migrations`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `msp_activities`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `msp_activity_cities`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `msp_activity_customers`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `neft_redemption_details`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `new_invoices_invoice_number_unique` (`invoice_number`),
  ADD KEY `new_invoices_secondary_customer_id_foreign` (`secondary_customer_id`),
  ADD KEY `new_invoices_created_by_foreign` (`created_by`),
  ADD KEY `new_invoices_approval_status_index` (`approval_status`),
  ADD KEY `new_invoices_approved_ss_by_index` (`approved_ss_by`),
  ADD KEY `new_invoices_approved_sales_by_index` (`approved_sales_by`),
  ADD KEY `new_invoices_approved_ho_by_index` (`approved_ho_by`),
  ADD KEY `new_invoices_rejected_by_index` (`rejected_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoice_approval_logs`
  ADD PRIMARY KEY (`id`),
  ADD KEY `new_invoice_approval_logs_new_invoice_id_index` (`new_invoice_id`),
  ADD KEY `new_invoice_approval_logs_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `new_joinings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `notes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `notes_user_id_index` (`user_id`),
  ADD KEY `notes_customer_id_index` (`customer_id`),
  ADD KEY `notes_purpose_index` (`purpose`),
  ADD KEY `notes_callstatus_index` (`callstatus`),
  ADD KEY `notes_status_id_index` (`status_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `notifications`
  ADD PRIMARY KEY (`id`),
  ADD KEY `notifications_type_index` (`type`),
  ADD KEY `notifications_user_id_index` (`user_id`),
  ADD KEY `notifications_customer_id_index` (`customer_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `oauth_auth_codes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `oauth_auth_codes_user_id_index` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `oauth_clients`
  ADD PRIMARY KEY (`id`),
  ADD KEY `oauth_clients_user_id_index` (`user_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `oauth_personal_access_clients`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `oauth_refresh_tokens`
  ADD PRIMARY KEY (`id`),
  ADD KEY `oauth_refresh_tokens_access_token_id_index` (`access_token_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `opening_stocks`
  ADD PRIMARY KEY (`id`),
  ADD KEY `opening_stocks_branch_id_foreign` (`branch_id`(8));");
            migrationBuilder.Sql(@"ALTER TABLE `opportunitie_statuses`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `orders`
  ADD PRIMARY KEY (`id`),
  ADD KEY `orders_buyer_id_index` (`buyer_id`),
  ADD KEY `orders_seller_id_index` (`seller_id`),
  ADD KEY `orders_total_qty_index` (`total_qty`),
  ADD KEY `orders_orderno_index` (`orderno`),
  ADD KEY `orders_order_date_index` (`order_date`),
  ADD KEY `orders_sub_total_index` (`sub_total`),
  ADD KEY `orders_grand_total_index` (`grand_total`),
  ADD KEY `orders_status_id_index` (`status_id`),
  ADD KEY `orders_address_id_index` (`address_id`),
  ADD KEY `orders_beatscheduleid_index` (`beatscheduleid`);");
            migrationBuilder.Sql(@"ALTER TABLE `order_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `order_details_product_id_index` (`product_id`),
  ADD KEY `order_details_product_detail_id_index` (`product_detail_id`),
  ADD KEY `order_details_quantity_index` (`quantity`),
  ADD KEY `order_details_price_index` (`price`),
  ADD KEY `order_details_line_total_index` (`line_total`),
  ADD KEY `order_details_status_id_index` (`status_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `order_schemes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `order_schemes_scheme_name_index` (`scheme_name`);");
            migrationBuilder.Sql(@"ALTER TABLE `order_scheme_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `order_scheme_details_order_scheme_id_index` (`order_scheme_id`),
  ADD KEY `order_scheme_details_product_id_index` (`product_id`),
  ADD KEY `order_scheme_details_category_id_index` (`category_id`),
  ADD KEY `order_scheme_details_subcategory_id_index` (`subcategory_id`),
  ADD KEY `order_scheme_details_points_index` (`points`);");
            migrationBuilder.Sql(@"ALTER TABLE `parent_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `parent_details_created_by_index` (`created_by`),
  ADD KEY `parent_details_updated_by_index` (`updated_by`),
  ADD KEY `parent_details_customer_id_index` (`customer_id`),
  ADD KEY `parent_details_parent_id_index` (`parent_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `password_resets`
  ADD KEY `password_resets_email_index` (`email`);");
            migrationBuilder.Sql(@"ALTER TABLE `payments`
  ADD PRIMARY KEY (`id`),
  ADD KEY `payments_user_id_index` (`user_id`),
  ADD KEY `payments_customer_id_index` (`customer_id`),
  ADD KEY `payments_customer_name_index` (`customer_name`),
  ADD KEY `payments_payment_date_index` (`payment_date`),
  ADD KEY `payments_payment_mode_index` (`payment_mode`),
  ADD KEY `payments_payment_type_index` (`payment_type`),
  ADD KEY `payments_bank_name_index` (`bank_name`),
  ADD KEY `payments_reference_no_index` (`reference_no`),
  ADD KEY `payments_amount_index` (`amount`),
  ADD KEY `payments_response_index` (`response`),
  ADD KEY `payments_description_index` (`description`),
  ADD KEY `payments_file_path_index` (`file_path`),
  ADD KEY `payments_status_id_index` (`status_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `payment_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `payment_details_payment_id_index` (`payment_id`),
  ADD KEY `payment_details_sales_id_index` (`sales_id`),
  ADD KEY `payment_details_invoice_no_index` (`invoice_no`),
  ADD KEY `payment_details_amount_index` (`amount`);");
            migrationBuilder.Sql(@"ALTER TABLE `payment_terms`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `personal_access_tokens`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `personal_access_tokens_token_unique` (`token`),
  ADD KEY `personal_access_tokens_tokenable_type_tokenable_id_index` (`tokenable_type`,`tokenable_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `pincodes`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `pincodes_city_id_pincode_unique` (`city_id`,`pincode`),
  ADD KEY `pincodes_pincode_index` (`pincode`),
  ADD KEY `pincodes_city_id_index` (`city_id`),
  ADD KEY `pincodes_created_by_index` (`created_by`),
  ADD KEY `pincodes_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `planned_sop_sale_data`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `planned_s_o_p_s`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_planning_month` (`planning_month`),
  ADD KEY `idx_division_id` (`division_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `power_bi_settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `primary_sales`
  ADD PRIMARY KEY (`id`),
  ADD KEY `primary_sales_invoiceno_index` (`invoiceno`),
  ADD KEY `primary_sales_invoice_date_index` (`invoice_date`),
  ADD KEY `primary_sales_quantity_index` (`quantity`),
  ADD KEY `primary_sales_rate_index` (`rate`),
  ADD KEY `primary_sales_net_amount_index` (`net_amount`),
  ADD KEY `primary_sales_total_amount_index` (`total_amount`);");
            migrationBuilder.Sql(@"ALTER TABLE `primary_schemes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `primary_schemes_scheme_name_index` (`scheme_name`);");
            migrationBuilder.Sql(@"ALTER TABLE `primary_schemes_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `primary_schemes_details_primary_scheme_id_index` (`primary_scheme_id`),
  ADD KEY `primary_schemes_details_product_id_index` (`product_id`),
  ADD KEY `primary_schemes_details_category_id_index` (`category_id`),
  ADD KEY `primary_schemes_details_subcategory_id_index` (`subcategory_id`),
  ADD KEY `primary_schemes_details_points_index` (`points`);");
            migrationBuilder.Sql(@"ALTER TABLE `products`
  ADD PRIMARY KEY (`id`),
  ADD KEY `products_product_name_index` (`product_name`),
  ADD KEY `products_display_name_index` (`display_name`),
  ADD KEY `products_description_index` (`description`),
  ADD KEY `products_subcategory_id_index` (`subcategory_id`),
  ADD KEY `products_category_id_index` (`category_id`),
  ADD KEY `products_brand_id_index` (`brand_id`),
  ADD KEY `products_product_image_index` (`product_image`),
  ADD KEY `products_unit_id_index` (`unit_id`),
  ADD KEY `products_created_by_index` (`created_by`),
  ADD KEY `products_updated_by_index` (`updated_by`),
  ADD KEY `products_specification_index` (`specification`),
  ADD KEY `products_part_no_index` (`part_no`),
  ADD KEY `products_product_no_index` (`product_no`),
  ADD KEY `products_model_no_index` (`model_no`);");
            migrationBuilder.Sql(@"ALTER TABLE `product_details`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `product_details_hsn_code_unique` (`hsn_code`),
  ADD UNIQUE KEY `product_details_ean_code_unique` (`ean_code`),
  ADD KEY `product_details_detail_title_index` (`detail_title`),
  ADD KEY `product_details_detail_description_index` (`detail_description`),
  ADD KEY `product_details_product_id_index` (`product_id`),
  ADD KEY `product_details_detail_image_index` (`detail_image`),
  ADD KEY `product_details_mrp_index` (`mrp`),
  ADD KEY `product_details_price_index` (`price`),
  ADD KEY `product_details_selling_price_index` (`selling_price`);");
            migrationBuilder.Sql(@"ALTER TABLE `redemptions`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `regions`
  ADD PRIMARY KEY (`id`),
  ADD KEY `regions_region_name_index` (`region_name`);");
            migrationBuilder.Sql(@"ALTER TABLE `resignations`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `resignation_check_lists`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `sales`
  ADD PRIMARY KEY (`id`),
  ADD KEY `sales_buyer_id_index` (`buyer_id`),
  ADD KEY `sales_seller_id_index` (`seller_id`),
  ADD KEY `sales_order_id_index` (`order_id`),
  ADD KEY `sales_total_qty_index` (`total_qty`),
  ADD KEY `sales_orderno_index` (`orderno`),
  ADD KEY `sales_sales_no_index` (`sales_no`),
  ADD KEY `sales_invoice_no_index` (`invoice_no`),
  ADD KEY `sales_invoice_date_index` (`invoice_date`),
  ADD KEY `sales_sub_total_index` (`sub_total`),
  ADD KEY `sales_grand_total_index` (`grand_total`),
  ADD KEY `sales_description_index` (`description`),
  ADD KEY `sales_status_id_index` (`status_id`),
  ADD KEY `sales_created_by_index` (`created_by`),
  ADD KEY `sales_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `salestargetcustomers`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `salestargetusers`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `sales_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `sales_details_sales_id_index` (`sales_id`),
  ADD KEY `sales_details_product_id_index` (`product_id`),
  ADD KEY `sales_details_product_detail_id_index` (`product_detail_id`),
  ADD KEY `sales_details_quantity_index` (`quantity`),
  ADD KEY `sales_details_price_index` (`price`),
  ADD KEY `sales_details_line_total_index` (`line_total`),
  ADD KEY `sales_details_status_id_index` (`status_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `sales_targets`
  ADD PRIMARY KEY (`id`),
  ADD KEY `sales_targets_userid_index` (`userid`),
  ADD KEY `sales_targets_startdate_index` (`startdate`),
  ADD KEY `sales_targets_enddate_index` (`enddate`),
  ADD KEY `sales_targets_amount_index` (`amount`),
  ADD KEY `sales_targets_achievement_index` (`achievement`),
  ADD KEY `sales_targets_created_by_index` (`created_by`),
  ADD KEY `sales_targets_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `sales_weightages`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `sap_stocks`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `scheme_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `scheme_details_scheme_id_index` (`scheme_id`),
  ADD KEY `scheme_details_product_id_index` (`product_id`),
  ADD KEY `scheme_details_category_id_index` (`category_id`),
  ADD KEY `scheme_details_subcategory_id_index` (`subcategory_id`),
  ADD KEY `scheme_details_minimum_index` (`active_point`),
  ADD KEY `scheme_details_maximum_index` (`provision_point`),
  ADD KEY `scheme_details_points_index` (`points`);");
            migrationBuilder.Sql(@"ALTER TABLE `scheme_headers`
  ADD PRIMARY KEY (`id`),
  ADD KEY `scheme_headers_scheme_name_index` (`scheme_name`),
  ADD KEY `scheme_headers_scheme_description_index` (`scheme_description`),
  ADD KEY `scheme_headers_start_date_index` (`start_date`),
  ADD KEY `scheme_headers_end_date_index` (`end_date`),
  ADD KEY `scheme_headers_scheme_image_index` (`scheme_image`);");
            migrationBuilder.Sql(@"ALTER TABLE `secondary_customers`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `secondary_customers_mobile_number_unique` (`mobile_number`);");
            migrationBuilder.Sql(@"ALTER TABLE `services`
  ADD PRIMARY KEY (`id`),
  ADD KEY `serial_no` (`serial_no`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_bills`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_bill_complaint_types`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_bill_product_details`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_categories`
  ADD PRIMARY KEY (`id`),
  ADD KEY `service_charge_categories_subcategory_name_index` (`category_name`),
  ADD KEY `service_charge_categories_subcategory_image_index` (`subcategory_image`),
  ADD KEY `service_charge_categories_division_id_index` (`division_id`),
  ADD KEY `service_charge_categories_created_by_index` (`created_by`),
  ADD KEY `service_charge_categories_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_charge_types`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_divisions`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_products`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_complaint_reasons`
  ADD PRIMARY KEY (`id`),
  ADD KEY `service_complaint_reasons_service_bill_complaint_id_foreign` (`service_bill_complaint_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `service_group_complaints`
  ADD PRIMARY KEY (`id`),
  ADD KEY `service_group_complaints_subcategory_id_foreign` (`subcategory_id`),
  ADD KEY `service_group_complaints_service_bill_complaint_id_foreign` (`service_bill_complaint_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `settings`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `states`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `states_country_id_state_name_unique` (`country_id`,`state_name`),
  ADD KEY `states_state_name_index` (`state_name`),
  ADD KEY `states_country_id_index` (`country_id`),
  ADD KEY `states_created_by_index` (`created_by`),
  ADD KEY `states_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `statuses`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `statuses_status_name_unique` (`status_name`),
  ADD KEY `statuses_display_name_index` (`display_name`),
  ADD KEY `statuses_created_by_index` (`created_by`),
  ADD KEY `statuses_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `subcategories`
  ADD PRIMARY KEY (`id`),
  ADD KEY `subcategories_subcategory_name_index` (`subcategory_name`),
  ADD KEY `subcategories_subcategory_image_index` (`subcategory_image`),
  ADD KEY `subcategories_category_id_index` (`category_id`),
  ADD KEY `subcategories_created_by_index` (`created_by`),
  ADD KEY `subcategories_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `supports`
  ADD PRIMARY KEY (`id`),
  ADD KEY `supports_subject_index` (`subject`),
  ADD KEY `supports_description_index` (`description`),
  ADD KEY `supports_full_name_index` (`full_name`),
  ADD KEY `supports_user_id_index` (`user_id`),
  ADD KEY `supports_status_id_index` (`status_id`),
  ADD KEY `supports_customer_id_index` (`customer_id`),
  ADD KEY `supports_assigned_to_index` (`assigned_to`);");
            migrationBuilder.Sql(@"ALTER TABLE `survey_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `survey_data_field_id_index` (`field_id`),
  ADD KEY `survey_data_customer_id_index` (`customer_id`),
  ADD KEY `survey_data_value_index` (`value`),
  ADD KEY `survey_data_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `tasks`
  ADD PRIMARY KEY (`id`),
  ADD KEY `tasks_user_id_index` (`user_id`),
  ADD KEY `tasks_title_index` (`title`),
  ADD KEY `tasks_descriptions_index` (`descriptions`),
  ADD KEY `tasks_datetime_index` (`datetime`),
  ADD KEY `tasks_reminder_index` (`reminder`),
  ADD KEY `tasks_customer_id_index` (`customer_id`),
  ADD KEY `tasks_status_id_index` (`status_id`),
  ADD KEY `tasks_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `task_assignments`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `task_comments`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `task_departments`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `task_priorities`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `task_projects`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `task_status_logs`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `tax_invoice_tax`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `tax_invoice_tds`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `tour_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `tour_details_tourid_index` (`tourid`),
  ADD KEY `tour_details_city_id_index` (`city_id`),
  ADD KEY `tour_details_visited_date_index` (`visited_date`),
  ADD KEY `tour_details_visited_cityid_index` (`visited_cityid`),
  ADD KEY `tour_details_last_visited_index` (`last_visited`);");
            migrationBuilder.Sql(@"ALTER TABLE `tour_programmes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `tour_programmes_date_index` (`date`),
  ADD KEY `tour_programmes_userid_index` (`userid`),
  ADD KEY `tour_programmes_town_index` (`town`),
  ADD KEY `tour_programmes_type_index` (`type`),
  ADD KEY `tour_programmes_status_index` (`status`),
  ADD KEY `tour_programmes_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `transaction_histories`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `unit_measures`
  ADD PRIMARY KEY (`id`),
  ADD KEY `unit_measures_unit_name_index` (`unit_name`),
  ADD KEY `unit_measures_unit_code_index` (`unit_code`),
  ADD KEY `unit_measures_created_by_index` (`created_by`),
  ADD KEY `unit_measures_updated_by_index` (`updated_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `user_activities`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_activities_userid_index` (`userid`),
  ADD KEY `user_activities_latitude_index` (`latitude`),
  ADD KEY `user_activities_longitude_index` (`longitude`),
  ADD KEY `user_activities_time_index` (`time`),
  ADD KEY `user_activities_address_index` (`address`),
  ADD KEY `user_activities_description_index` (`description`),
  ADD KEY `user_activities_type_index` (`type`);");
            migrationBuilder.Sql(@"ALTER TABLE `user_daily_lat_longs`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `user_live_locations`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_live_locations_userid_index` (`userid`),
  ADD KEY `user_live_locations_latitude_index` (`latitude`),
  ADD KEY `user_live_locations_longitude_index` (`longitude`),
  ADD KEY `user_live_locations_time_index` (`time`),
  ADD KEY `user_live_locations_address_index` (`address`);");
            migrationBuilder.Sql(@"ALTER TABLE `user_logins`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_logins_user_id_index` (`user_id`),
  ADD KEY `user_logins_entry_from_index` (`entry_from`),
  ADD KEY `user_logins_provider_index` (`provider`),
  ADD KEY `user_logins_mobile_index` (`mobile`),
  ADD KEY `user_logins_login_at_index` (`login_at`),
  ADD KEY `user_logins_logout_at_index` (`logout_at`);");
            migrationBuilder.Sql(@"ALTER TABLE `user_pms_remarks`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `visitors`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `visit_reports`
  ADD PRIMARY KEY (`id`),
  ADD KEY `visit_reports_checkin_id_index` (`checkin_id`),
  ADD KEY `visit_reports_user_id_index` (`user_id`),
  ADD KEY `visit_reports_customer_id_index` (`customer_id`),
  ADD KEY `visit_reports_visit_type_id_index` (`visit_type_id`),
  ADD KEY `visit_reports_report_title_index` (`report_title`),
  ADD KEY `visit_reports_description_index` (`description`),
  ADD KEY `visit_reports_visit_image_index` (`visit_image`),
  ADD KEY `visit_reports_next_visit_index` (`next_visit`),
  ADD KEY `visit_reports_status_id_index` (`status_id`),
  ADD KEY `visit_reports_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `visit_types`
  ADD PRIMARY KEY (`id`),
  ADD KEY `visit_types_type_name_index` (`type_name`),
  ADD KEY `visit_types_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `wallets`
  ADD PRIMARY KEY (`id`),
  ADD KEY `wallets_customer_id_index` (`customer_id`),
  ADD KEY `wallets_scheme_id_index` (`scheme_id`),
  ADD KEY `wallets_schemedetail_id_index` (`schemedetail_id`),
  ADD KEY `wallets_points_index` (`points`),
  ADD KEY `wallets_point_type_index` (`point_type`),
  ADD KEY `wallets_invoice_amount_index` (`invoice_amount`),
  ADD KEY `wallets_invoice_no_index` (`invoice_no`),
  ADD KEY `wallets_coupon_code_index` (`coupon_code`),
  ADD KEY `wallets_invoice_date_index` (`invoice_date`),
  ADD KEY `wallets_transaction_type_index` (`transaction_type`),
  ADD KEY `wallets_sales_id_index` (`sales_id`),
  ADD KEY `wallets_status_id_index` (`status_id`),
  ADD KEY `wallets_checkinid_index` (`checkinid`),
  ADD KEY `wallets_quantity_index` (`quantity`),
  ADD KEY `wallets_userid_index` (`userid`);");
            migrationBuilder.Sql(@"ALTER TABLE `wallet_details`
  ADD PRIMARY KEY (`id`),
  ADD KEY `wallet_details_wallet_id_index` (`wallet_id`),
  ADD KEY `wallet_details_points_index` (`points`),
  ADD KEY `wallet_details_coupon_code_index` (`coupon_code`),
  ADD KEY `wallet_details_product_id_index` (`product_id`),
  ADD KEY `wallet_details_category_id_index` (`category_id`),
  ADD KEY `wallet_details_subcategory_id_index` (`subcategory_id`),
  ADD KEY `wallet_details_quantity_index` (`quantity`),
  ADD KEY `wallet_details_status_id_index` (`status_id`);");
            migrationBuilder.Sql(@"ALTER TABLE `ware_houses`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `warranty_activations`
  ADD PRIMARY KEY (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `warranty_timelines`
  ADD PRIMARY KEY (`id`),
  ADD KEY `warranty_timelines_warranty_id_index` (`warranty_id`),
  ADD KEY `warranty_timelines_created_by_index` (`created_by`);");
            migrationBuilder.Sql(@"ALTER TABLE `active_customer_processes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `active_customer_process_steps`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `addresses`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `appraisals`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `attachments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `attendances`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `beats`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `beat_customers`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `beat_schedules`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `beat_users`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `branches`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `branchwise_targets`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `branch_oprning_quantities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `branch_stocks`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `brands`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `call_logs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `categories`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `check_in`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `check_in_drafts`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `cities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `claim_generations`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `claim_generation_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `complaints`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `complaint_timelines`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `complaint_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `complaint_work_dones`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `comp_off_leaves`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `countries`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `coupons`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `coupon_profiles`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_custom_fields`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_custom_field_values`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_outstantings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_processes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_process_steps`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `customer_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `custom_pdf_values`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `damage_entries`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `dealer_appointments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `dealer_appointment_kycs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `dealer_portal_settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `deal_ins`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `departments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `designations`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `districts`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `divisions`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `employee_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `end_users`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `estimates`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `estimate_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `expenses`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `expenses_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `expense_logs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `failed_jobs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `fields`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `fieldsdata`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `field_konnect_app_settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `firm_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `gamifications`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `geo_locator_settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `gifts`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `giftsubcategories`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `gift_brands`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `gift_categories`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `gift_models`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `gift_redemption_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `holidays`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `invoices`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `invoice_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `invoice_labels`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `invoice_settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `jobs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `leads`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_check_in`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_contacts`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_logs`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_notes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_notifications`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_opportunities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `lead_tasks`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `leaves`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `loyalty_app_settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `marketings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `marketing_activities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligences_fielddatas`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligences_fields`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligence_serveys`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `market_intelligence_servey_data`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `master_distributors`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `media`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `migrations`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `msp_activities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `msp_activity_cities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `msp_activity_customers`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `neft_redemption_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoice_approval_logs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `new_joinings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `notes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `notifications`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `oauth_personal_access_clients`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `opening_stocks`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `opportunitie_statuses`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `orders`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `order_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `order_schemes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `order_scheme_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `parent_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `payments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `payment_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `payment_terms`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `personal_access_tokens`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `pincodes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `planned_sop_sale_data`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `planned_s_o_p_s`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `power_bi_settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `primary_sales`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `primary_schemes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `primary_schemes_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `products`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `product_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `redemptions`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `regions`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `resignations`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `resignation_check_lists`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `sales`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `salestargetcustomers`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `salestargetusers`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `sales_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `sales_targets`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `sales_weightages`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `sap_stocks`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `scheme_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `scheme_headers`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `secondary_customers`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `services`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_bills`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_bill_complaint_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_bill_product_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_categories`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_charge_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_divisions`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_charge_products`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_complaint_reasons`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `service_group_complaints`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `settings`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `states`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `statuses`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `subcategories`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `supports`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `survey_data`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `tasks`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `task_assignments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `task_comments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `task_departments`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `task_priorities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `task_projects`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `task_status_logs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `tax_invoice_tax`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `tax_invoice_tds`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `tour_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `tour_programmes`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `transaction_histories`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `unit_measures`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `user_activities`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `user_daily_lat_longs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `user_live_locations`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `user_logins`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `user_pms_remarks`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `visitors`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `visit_reports`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `visit_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `wallets`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `wallet_details`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `ware_houses`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `warranty_activations`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `warranty_timelines`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;");
            migrationBuilder.Sql(@"ALTER TABLE `active_customer_processes`
  ADD CONSTRAINT `active_customer_processes_assigned_by_foreign` FOREIGN KEY (`assigned_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `active_customer_processes_customer_id_foreign` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `active_customer_processes_process_id_foreign` FOREIGN KEY (`process_id`) REFERENCES `customer_processes` (`id`) ON DELETE CASCADE;");
            migrationBuilder.Sql(@"ALTER TABLE `active_customer_process_steps`
  ADD CONSTRAINT `active_customer_process_steps_active_customer_process_id_foreign` FOREIGN KEY (`active_customer_process_id`) REFERENCES `active_customer_processes` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `active_customer_process_steps_completed_by_foreign` FOREIGN KEY (`completed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  ADD CONSTRAINT `active_customer_process_steps_customer_process_step_id_foreign` FOREIGN KEY (`customer_process_step_id`) REFERENCES `customer_process_steps` (`id`) ON DELETE CASCADE;");
            migrationBuilder.Sql(@"ALTER TABLE `addresses`
  ADD CONSTRAINT `addresses_city_id_foreign` FOREIGN KEY (`city_id`) REFERENCES `cities` (`id`),
  ADD CONSTRAINT `addresses_country_id_foreign` FOREIGN KEY (`country_id`) REFERENCES `countries` (`id`),
  ADD CONSTRAINT `addresses_district_id_foreign` FOREIGN KEY (`district_id`) REFERENCES `districts` (`id`),
  ADD CONSTRAINT `addresses_pincode_id_foreign` FOREIGN KEY (`pincode_id`) REFERENCES `pincodes` (`id`),
  ADD CONSTRAINT `addresses_state_id_foreign` FOREIGN KEY (`state_id`) REFERENCES `states` (`id`),
  ADD CONSTRAINT `addresses_user_id_foreign` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE;");
            migrationBuilder.Sql(@"ALTER TABLE `beats`
  ADD CONSTRAINT `beats_country_id_foreign` FOREIGN KEY (`country_id`) REFERENCES `countries` (`id`),
  ADD CONSTRAINT `beats_created_by_foreign` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `beats_state_id_foreign` FOREIGN KEY (`state_id`) REFERENCES `states` (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `beat_customers`
  ADD CONSTRAINT `beat_customers_beat_id_foreign` FOREIGN KEY (`beat_id`) REFERENCES `beats` (`id`),
  ADD CONSTRAINT `beat_customers_customer_id_foreign` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `beat_users`
  ADD CONSTRAINT `beat_users_beat_id_foreign` FOREIGN KEY (`beat_id`) REFERENCES `beats` (`id`),
  ADD CONSTRAINT `beat_users_user_id_foreign` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`);");
            migrationBuilder.Sql(@"ALTER TABLE `new_invoices`
  ADD CONSTRAINT `new_invoices_created_by_foreign` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `new_invoices_secondary_customer_id_foreign` FOREIGN KEY (`secondary_customer_id`) REFERENCES `customers` (`id`) ON DELETE CASCADE;");
            migrationBuilder.Sql(@"SET FOREIGN_KEY_CHECKS=1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET FOREIGN_KEY_CHECKS=0;");
            migrationBuilder.Sql(@"ALTER TABLE `mobile_user_login_details` DROP COLUMN IF EXISTS `active`;");
            migrationBuilder.Sql(@"ALTER TABLE `user_details` DROP COLUMN IF EXISTS `aadhar_card_image`;");
            migrationBuilder.Sql(@"ALTER TABLE `user_details` DROP COLUMN IF EXISTS `pan_card_image`;");
            migrationBuilder.Sql(@"ALTER TABLE `customers` DROP COLUMN IF EXISTS `creation_date`;");
            migrationBuilder.Sql(@"ALTER TABLE `customers` DROP COLUMN IF EXISTS `working_status`;");
            migrationBuilder.Sql(@"ALTER TABLE `users` DROP COLUMN IF EXISTS `login_at`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `warranty_timelines`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `warranty_activations`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `ware_houses`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `wallet_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `wallets`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `visit_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `visit_reports`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `visitors`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `user_pms_remarks`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `user_logins`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `user_live_locations`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `user_daily_lat_longs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `user_activities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `unit_measures`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `transaction_histories`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `tour_programmes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `tour_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `tax_invoice_tds`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `tax_invoice_tax`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `task_status_logs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `task_projects`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `task_priorities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `task_departments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `task_comments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `task_assignments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `tasks`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `survey_data`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `supports`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `subcategories`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `statuses`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `states`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `shipping_addresses`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_group_complaints`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_complaint_reasons`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_charge_products`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_charge_divisions`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_charge_charge_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_charge_categories`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_bill_product_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_bill_complaint_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `service_bills`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `services`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `secondary_customers`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `scheme_headers`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `scheme_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `sap_stocks`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `sales_weightages`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `sales_targets`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `sales_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `salestargetusers`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `salestargetcustomers`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `sales`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `resignation_check_lists`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `resignations`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `regions`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `redemptions`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `product_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `products`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `primary_schemes_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `primary_schemes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `primary_sales`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `power_bi_settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `planned_s_o_p_s`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `planned_sop_sale_data`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `pincodes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `personal_access_tokens`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `payment_terms`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `payment_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `payments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `password_resets`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `parent_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `order_scheme_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `order_schemes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `order_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `orders`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `opportunitie_statuses`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `opening_stocks`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `oauth_refresh_tokens`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `oauth_personal_access_clients`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `oauth_clients`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `oauth_auth_codes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `notifications`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `notes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `new_joinings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `new_invoice_approval_logs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `new_invoices`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `neft_redemption_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `msp_activity_customers`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `msp_activity_cities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `msp_activities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `migrations`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `media`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `master_distributors`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `market_intelligence_servey_data`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `market_intelligence_serveys`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `market_intelligences_fields`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `market_intelligences_fielddatas`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `marketing_activities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `marketings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `loyalty_app_settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `leaves`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_tasks`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_opportunities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_notifications`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_notes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_logs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_contacts`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `lead_check_in`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `leads`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `jobs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `invoice_settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `invoice_labels`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `invoice_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `invoices`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `holidays`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `gift_redemption_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `gift_models`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `gift_categories`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `gift_brands`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `giftsubcategories`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `gifts`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `geo_locator_settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `gamifications`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `firm_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `field_konnect_app_settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `fieldsdata`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `fields`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `failed_jobs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `expense_logs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `expenses_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `expenses`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `estimate_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `estimates`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `end_users`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `employee_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `divisions`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `districts`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `designations`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `departments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `deal_ins`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `dealer_portal_settings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `dealer_appointment_kycs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `dealer_appointments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `damage_entries`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `custom_pdf_values`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_process_steps`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_processes`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_outstantings`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_custom_field_values`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `customer_custom_fields`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `coupon_profiles`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `coupons`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `countries`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `comp_off_leaves`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `complaint_work_dones`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `complaint_types`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `complaint_timelines`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `complaints`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `claim_generation_details`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `claim_generations`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `cities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `check_in_drafts`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `check_in`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `categories`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `call_logs`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `brands`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `branch_stocks`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `branch_oprning_quantities`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `branchwise_targets`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `branches`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `beat_users`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `beat_schedules`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `beat_customers`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `beats`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `attendances`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `attachments`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `appraisals`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `addresses`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `active_customer_process_steps`;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `active_customer_processes`;");
            migrationBuilder.Sql(@"SET FOREIGN_KEY_CHECKS=1;");
        }
    }
}

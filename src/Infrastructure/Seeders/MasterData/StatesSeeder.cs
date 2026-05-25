using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeders.MasterData;

public static class StatesSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        const string sql1 = """""""
INSERT INTO `states` (`id`, `active`, `state_name`, `country_id`, `gst_code`, `created_by`, `updated_by`, `deleted_at`, `created_at`, `updated_at`) VALUES
(1, 'Y', 'Madhya Pradesh', 1, '23', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(2, 'Y', 'Lakshadweep', 1, '31', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(3, 'Y', 'Maharashtra', 1, '27', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(4, 'Y', 'Manipur', 1, '14', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(5, 'Y', 'Meghalaya', 1, '17', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(6, 'Y', 'Mizoram', 1, '15', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(7, 'Y', 'Nagaland', 1, '13', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(8, 'Y', 'Orissa', 1, '21', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(9, 'Y', 'Pondicherry', 1, '34', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(10, 'Y', 'Punjab', 1, '3', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(11, 'Y', 'Rajasthan', 1, '8', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(12, 'Y', 'Sikkim', 1, '11', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(13, 'Y', 'Tamil Nadu', 1, '33', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(14, 'Y', 'Telangana', 1, '36', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(15, 'Y', 'Tripura', 1, '16', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(16, 'Y', 'Uttar Pradesh', 1, '9', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(17, 'Y', 'Uttarakhand', 1, '5', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(18, 'Y', 'West Bengal', 1, '19', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(19, 'Y', 'Delhi', 1, '7', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(20, 'Y', 'Andhra Pradesh', 1, '28', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(21, 'Y', 'Arunachal Pradesh', 1, '12', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(22, 'Y', 'Assam', 1, '18', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(23, 'Y', 'Bihar', 1, '10', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(24, 'Y', 'Chandigarh', 1, '4', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(25, 'Y', 'Chhattisgarh', 1, '22', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(26, 'Y', 'Dadra & Nagar Haveli', 1, '26', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(27, 'Y', 'Daman And Diu', 1, '25', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(28, 'Y', 'Andaman And Nicobar', 1, '35', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(29, 'Y', 'Goa', 1, '30', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(30, 'Y', 'Gujarat', 1, '24', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(31, 'Y', 'Haryana', 1, '6', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(32, 'Y', 'Himachal Pradesh', 1, '2', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(33, 'Y', 'Jammu And Kashmir', 1, '1', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(34, 'Y', 'Jharkhand', 1, '20', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(35, 'Y', 'Karnataka', 1, '29', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14'),
(36, 'Y', 'Kerala', 1, '32', 1, NULL, NULL, '2023-10-26 09:18:41', '2025-09-03 12:04:14')
ON DUPLICATE KEY UPDATE `active` = VALUES(`active`), `state_name` = VALUES(`state_name`), `country_id` = VALUES(`country_id`), `gst_code` = VALUES(`gst_code`), `created_by` = VALUES(`created_by`), `updated_by` = VALUES(`updated_by`), `deleted_at` = VALUES(`deleted_at`), `created_at` = VALUES(`created_at`), `updated_at` = VALUES(`updated_at`);
""""""";
        await db.Database.ExecuteSqlRawAsync(sql1, cancellationToken);

    }
}

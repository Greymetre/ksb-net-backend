using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeders.MasterData;

public static class CountriesSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        const string sql1 = """""""
INSERT INTO `countries` (`id`, `active`, `country_name`, `created_by`, `updated_by`, `deleted_at`, `created_at`, `updated_at`) VALUES
(1, 'Y', 'India', 1, NULL, NULL, '2023-10-26 03:47:11', '2024-09-10 12:41:04')
ON DUPLICATE KEY UPDATE `active` = VALUES(`active`), `country_name` = VALUES(`country_name`), `created_by` = VALUES(`created_by`), `updated_by` = VALUES(`updated_by`), `deleted_at` = VALUES(`deleted_at`), `created_at` = VALUES(`created_at`), `updated_at` = VALUES(`updated_at`);
""""""";
        await db.Database.ExecuteSqlRawAsync(sql1, cancellationToken);

    }
}

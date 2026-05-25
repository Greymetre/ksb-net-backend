using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeders.MasterData;

public static class MasterDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS=0;", cancellationToken);

        try
        {
            await CountriesSeeder.SeedAsync(db, cancellationToken);
            await StatesSeeder.SeedAsync(db, cancellationToken);
            await DistrictsSeeder.SeedAsync(db, cancellationToken);
            await CitiesSeeder.SeedAsync(db, cancellationToken);
            await PincodesSeeder.SeedAsync(db, cancellationToken);
            await PermissionsSeeder.SeedAsync(db, cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS=1;", cancellationToken);
        }
    }
}

using LanguageLearning.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public sealed class PlacementSchemaUpgradeTests
{
    [Fact]
    public async Task ExistingDatabase_GainsPlacementTablesAndSupportsQueries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE users (Id TEXT NOT NULL PRIMARY KEY)";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await LegacySchemaUpgrade.EnsurePlacementReviewTablesAsync(context);
        await LegacySchemaUpgrade.EnsurePlacementReviewTablesAsync(context);

        Assert.Empty(await context.PlacementAttempts.ToListAsync());
        Assert.Empty(await context.PlacementAudios.ToListAsync());
    }
}

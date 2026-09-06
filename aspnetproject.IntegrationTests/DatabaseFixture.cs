using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using aspnetproject.Data;
using aspnetproject.IntegrationTests.Fixtures;

namespace aspnetproject.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString = "Server=localhost;Database=TestSocialMediaWebApiDb;Trusted_Connection=True;TrustServerCertificate=True";

    private Respawner _respawner = null!;
    private DbConnection _connection = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using (var db = new ApplicationDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        _connection = new SqlConnection(ConnectionString);
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" },
            DbAdapter = DbAdapter.SqlServer
        });

        await ResetAndSeedAsync(options);
    }

    public async Task ResetAndSeedAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await _respawner.ResetAsync(_connection);

        await using var db = new ApplicationDbContext(options);
        await TestDataSeeder.SeedAsync(db);
    }

    public DbContextOptions<ApplicationDbContext> GetOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

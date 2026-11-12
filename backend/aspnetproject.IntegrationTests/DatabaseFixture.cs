using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using aspnetproject.Data;
using aspnetproject.IntegrationTests.Fixtures;

namespace aspnetproject.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    // The same variable the app under test reads, so both sides agree. Set where Windows authentication is unavailable
    // (scripts/test-linux.sh sets it); unset, the suite uses the local default below.
    private const string ConnectionStringVariable = "ConnectionStrings__DefaultConnection";
    private const string LocalConnectionString    = "Server=localhost;Database=TestSocialMediaWebApiDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? LocalConnectionString;

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

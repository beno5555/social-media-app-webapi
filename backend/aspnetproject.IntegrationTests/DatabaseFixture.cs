using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using aspnetproject.Data;
using aspnetproject.IntegrationTests.Fixtures;

namespace aspnetproject.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    // Set where Windows authentication is unavailable (scripts/test-linux.sh sets it); unset, the suite uses the local
    // default below. CustomWebApplicationFactory hands ConnectionString to the app under test, so both sides always agree.
    private const string ConnectionStringVariable    = "ConnectionStrings__DefaultConnection";
    private const string LocalConnectionStringFormat = "Server=localhost;Database={0};Trusted_Connection=True;TrustServerCertificate=True";
    private const string LocalDatabaseName           = "TestSocialMediaWebApiDb";
    private const string GitEntryName                = ".git";
    private const char   DatabaseNameSeparator       = '_';

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable(ConnectionStringVariable)
        ?? string.Format(LocalConnectionStringFormat, GetLocalDatabaseName());

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

    // A linked git worktree (claude --worktree) gets its own database named after its folder, so suites running in two
    // checkouts at once never wipe each other's data. The main checkout keeps the plain name.
    private static string GetLocalDatabaseName()
    {
        string databaseName = LocalDatabaseName;
        DirectoryInfo? checkoutRoot = FindCheckoutRoot();
        bool isLinkedWorktree = checkoutRoot is not null && File.Exists(Path.Combine(checkoutRoot.FullName, GitEntryName));

        if (isLinkedWorktree)
        {
            string suffix = new string(checkoutRoot!.Name
                .Select(character => char.IsAsciiLetterOrDigit(character) ? character : DatabaseNameSeparator)
                .ToArray());
            databaseName = $"{LocalDatabaseName}{DatabaseNameSeparator}{suffix}";
        }

        return databaseName;
    }

    // The nearest folder above the test binaries that holds a .git entry: a folder in the main checkout, a file in a linked worktree.
    private static DirectoryInfo? FindCheckoutRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        bool found = false;

        while (directory is not null && !found)
        {
            found = Path.Exists(Path.Combine(directory.FullName, GitEntryName));
            if (!found)
            {
                directory = directory.Parent;
            }
        }

        return directory;
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

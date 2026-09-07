using aspnetproject.Data;
using aspnetproject.Data.Seed;

namespace aspnetproject.Extensions;

public static class TestSeedExtensions
{
    public static void TestSeedEndpoint(this WebApplication app)
    {
        app.MapPost("/dev/seed", async (ApplicationDbContext db) =>
        {
            await new BulkDataSeeder(db).SeedAsync(300);
            return Results.Ok("Seeded");
        });
    }
}
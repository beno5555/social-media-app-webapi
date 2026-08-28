using aspnetproject.Data.Models;
using aspnetproject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace aspnetproject.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Comment>    Comments    { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<Message>    Messages    { get; set; }
    public DbSet<Post>       Posts       { get; set; }
    public DbSet<User>       Users       { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.LastUpdatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
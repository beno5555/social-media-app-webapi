using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Post>         Posts         { get; set; }
    public DbSet<Comment>      Comments      { get; set; }
    public DbSet<Friendship>   Friendships   { get; set; }
    public DbSet<Message>      Messages      { get; set; }
    public DbSet<User>         Users         { get; set; }
    public DbSet<Role>         Roles         { get; set; }
    public DbSet<UserRole>     UserRoles     { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Log>          Logs          { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
using aspnetproject.Common.ProjectConstants.Enums;
using Microsoft.EntityFrameworkCore;
using aspnetproject.Data;
using aspnetproject.Data.Models; 

namespace aspnetproject.IntegrationTests.Fixtures;

public static class TestDataSeeder
{
    private const string SharedPasswordHash = "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=";
    private const string SharedPasswordSalt = "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=";
    public const string SharedPlaintextPassword = "password123"; 

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await InsertWithIdentityAsync(db, "Users", async () =>
        {
            db.Users.AddRange(
                new User
                {
                    Id = 1, Username = "sandro_beno", Email = "benashvilisandro91@gmail.com", Bio = "Admin",
                    DateOfBirth = new DateTime(2000, 8, 16), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt, CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 2, Username = "alice", Email = "alice@example.com", Bio = "Primary test caller",
                    DateOfBirth = new DateTime(2000, 1, 1), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt, CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 3, Username = "bob", Email = "bob@example.com", Bio = "Alice's accepted friend",
                    DateOfBirth = new DateTime(2000, 1, 1), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt, CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 4, Username = "charlie", Email = "charlie@example.com", Bio = "Unrelated active user",
                    DateOfBirth = new DateTime(2000, 1, 1), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt, CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 5, Username = "diana", Email = "diana@example.com", Bio = "Pending friendship user",
                    DateOfBirth = new DateTime(2000, 1, 1), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt, CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 6, Username = "erin_deactivated", Email = "erin@example.com", Bio = "Deactivated fixture user",
                    DateOfBirth = new DateTime(2000, 1, 1), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt,
                    AccountDeactivatedAt = new DateTime(2026, 9, 6, 11, 50, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 7, Username = "frank_deleted", Email = "frank@example.com", Bio = "Soft-deleted fixture user",
                    DateOfBirth = new DateTime(2000, 1, 1), PasswordHash = SharedPasswordHash,
                    PasswordSalt = SharedPasswordSalt,
                    AccountDeletedAt = new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        });
        
        await InsertWithIdentityAsync(db, "Roles", async () =>
        {
            db.Roles.AddRange(
                new Role
                {
                    Id = 1, 
                    Name = nameof(RoleName.User),
                    CreatedAt = DateTime.UtcNow, 
                    LastUpdatedAt = null
                },
                new Role
                {
                    Id = 2, 
                    Name = nameof(RoleName.Administrator),
                    CreatedAt = DateTime.UtcNow, 
                    LastUpdatedAt = null
                }
            );

            await db.SaveChangesAsync();
        });

        db.UserRoles.AddRange(
            new UserRole { UserId = 1, RoleId = 1 },
            new UserRole { UserId = 1, RoleId = 2 }
        );
        await db.SaveChangesAsync();

        await InsertWithIdentityAsync(db, "Friendships", async () =>
        {
            db.Friendships.AddRange(
                new Friendship
                {
                    Id = 1, RequesterUserId = 2, AddresseeUserId = 3, FriendshipStatus = FriendshipStatus.Accepted,
                    SentAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
                },
                new Friendship
                {
                    Id = 2, RequesterUserId = 2, AddresseeUserId = 5, FriendshipStatus = FriendshipStatus.Pending,
                    SentAt = DateTime.UtcNow
                },
                new Friendship
                {
                    Id = 3, RequesterUserId = 4, AddresseeUserId = 2, FriendshipStatus = FriendshipStatus.Pending,
                    SentAt = DateTime.UtcNow
                },
                new Friendship
                {
                    Id = 4, RequesterUserId = 3, AddresseeUserId = 4, FriendshipStatus = FriendshipStatus.Declined,
                    SentAt = DateTime.UtcNow, LastUpdatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        });

        await InsertWithIdentityAsync(db, "Posts", async () =>
        {
            db.Posts.AddRange(
                new Post
                {
                    Id = 1, UserId = 2, PostTitle = "Alice Post", PostContent = "Alice owns this post.",
                    CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 2, UserId = 3, PostTitle = "Bob Post",
                    PostContent = "Bob's post should appear in Alice's feed.", CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 3, UserId = 4, PostTitle = "Charlie Post",
                    PostContent = "Charlie's public post should not appear in Alice's friend feed.",
                    CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 4, UserId = 6, PostTitle = "Erin Deactivated Post",
                    PostContent = "Post owned by a deactivated user.", CreatedAt = DateTime.UtcNow
                },
                new Post
                {
                    Id = 5, UserId = 7, PostTitle = "Frank Deleted Post",
                    PostContent = "Post owned by a soft-deleted user.", CreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        });

        await InsertWithIdentityAsync(db, "Messages", async () =>
        {
            db.Messages.AddRange(
                new Message
                {
                    Id = 1, SenderUserId = 3, ReceiverUserId = 2,
                    MessageContent = "Old unread message from Bob to Alice.", Seen = false, CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    Id = 2, SenderUserId = 2, ReceiverUserId = 3, MessageContent = "Read message from Alice to Bob.",
                    Seen = true, SeenAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    Id = 3, SenderUserId = 2, ReceiverUserId = 3, MessageContent = "Latest message from Alice to Bob.",
                    Seen = false, CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    Id = 4, SenderUserId = 3, ReceiverUserId = 2,
                    MessageContent = "Latest unread message from Bob to Alice.", Seen = false,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        });

        await InsertWithIdentityAsync(db, "Comments", async () =>
        {
            db.Comments.AddRange(
                new Comment
                {
                    Id = 1, CommenterUserId = 2, PostId = 2, CommentContent = "Alice comments on Bob's post.",
                    CreatedAt = DateTime.UtcNow
                },
                new Comment
                {
                    Id = 2, CommenterUserId = 3, PostId = 2, CommentContent = "Bob comments on his own post.",
                    CreatedAt = DateTime.UtcNow
                },
                new Comment
                {
                    Id = 3, CommenterUserId = 4, PostId = 2, CommentContent = "Charlie comments on Bob's post.",
                    CreatedAt = DateTime.UtcNow
                },
                new Comment
                {
                    Id = 4, CommenterUserId = 3, PostId = 1, CommentContent = "Bob comments on Alice's post.",
                    CreatedAt = DateTime.UtcNow
                },
                new Comment
                {
                    Id = 5, CommenterUserId = null, PostId = 1,
                    CommentContent = "Orphaned comment with no active author.", CreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        });
        
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Users', RESEED, 7)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Friendships', RESEED, 4)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Posts', RESEED, 5)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Messages', RESEED, 4)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Comments', RESEED, 5)");
    }

    private static async Task InsertWithIdentityAsync(ApplicationDbContext db, string tableName, Func<Task> insertAction)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON");
        await insertAction();
        await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");

        await transaction.CommitAsync();
    }
}

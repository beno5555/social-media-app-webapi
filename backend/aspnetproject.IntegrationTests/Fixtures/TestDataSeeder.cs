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
                    Id = 5, Username = "diana", Email = "benashvilisandro780@gmail.com", Bio = "Pending friendship user",
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
                },
                // Makes bob a mutual friend of alice and diana, who are not friends themselves.
                new Friendship
                {
                    Id = 5, RequesterUserId = 3, AddresseeUserId = 5, FriendshipStatus = FriendshipStatus.Accepted,
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
                    PostContent = "Charlie's post appears in Alice's feed although they are not friends.",
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
                },
                // Feed ranking posts, at fixed past times. Within each group the other two ranking factors are equal,
                // and the ids run against the expected order, so the newest-id tie-break alone cannot produce it.
                // Connection, at 09:00 with no activity: bob is alice's friend, diana shares bob with her, charlie is a stranger.
                new Post
                {
                    Id = 6, UserId = 3, PostTitle = "Bob Ranked Post", PostContent = "Friend tier.",
                    CreatedAt = new DateTime(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc)
                },
                new Post
                {
                    Id = 7, UserId = 5, PostTitle = "Diana Ranked Post", PostContent = "Mutual friend tier.",
                    CreatedAt = new DateTime(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc)
                },
                new Post
                {
                    Id = 8, UserId = 4, PostTitle = "Charlie Ranked Post", PostContent = "Stranger tier.",
                    CreatedAt = new DateTime(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc)
                },
                // Activity, charlie's at 08:00: post 9 has one like and one comment, post 10 has none.
                new Post
                {
                    Id = 9, UserId = 4, PostTitle = "Charlie Busy Post", PostContent = "Liked and commented.",
                    CreatedAt = new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc)
                },
                new Post
                {
                    Id = 10, UserId = 4, PostTitle = "Charlie Quiet Post", PostContent = "No activity.",
                    CreatedAt = new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc)
                },
                // Age, charlie's with no activity: post 11 at 07:00 is newer than post 12 at 06:00.
                new Post
                {
                    Id = 11, UserId = 4, PostTitle = "Charlie Newer Post", PostContent = "Written at seven.",
                    CreatedAt = new DateTime(2026, 9, 6, 7, 0, 0, DateTimeKind.Utc)
                },
                new Post
                {
                    Id = 12, UserId = 4, PostTitle = "Charlie Older Post", PostContent = "Written at six.",
                    CreatedAt = new DateTime(2026, 9, 6, 6, 0, 0, DateTimeKind.Utc)
                },
                // Tie: the same score as post 10 (charlie's, 08:00, no activity).
                new Post
                {
                    Id = 13, UserId = 4, PostTitle = "Charlie Twin Post", PostContent = "Scores the same as the quiet post.",
                    CreatedAt = new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc)
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
                },
                // A fixed time in the past, so it is always older than Constants.EditMessageWindow.
                new Message
                {
                    Id = 5, SenderUserId = 2, ReceiverUserId = 3,
                    MessageContent = "Old message from Alice to Bob, past the edit window.", Seen = true,
                    SeenAt = new DateTime(2026, 9, 1, 10, 5, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)
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
                },
                new Comment
                {
                    Id = 6, CommenterUserId = 5, PostId = 9, CommentContent = "Diana comments on Charlie's busy post.",
                    CreatedAt = new DateTime(2026, 9, 6, 8, 30, 0, DateTimeKind.Utc)
                }
            );

            await db.SaveChangesAsync();
        });

        // Fixed past times that rise with the id, so the newest-first order of likers is known
        // and likes written while a test runs are always the newest.
        await InsertWithIdentityAsync(db, "PostLikes", async () =>
        {
            db.PostLikes.AddRange(
                new PostLike { Id = 1, PostId = 2, UserId = 2, CreatedAt = new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc) },
                new PostLike { Id = 2, PostId = 2, UserId = 4, CreatedAt = new DateTime(2026, 9, 6, 10, 1, 0, DateTimeKind.Utc) },
                new PostLike { Id = 3, PostId = 2, UserId = 6, CreatedAt = new DateTime(2026, 9, 6, 10, 2, 0, DateTimeKind.Utc) },
                new PostLike { Id = 4, PostId = 1, UserId = 3, CreatedAt = new DateTime(2026, 9, 6, 10, 3, 0, DateTimeKind.Utc) },
                new PostLike { Id = 5, PostId = 9, UserId = 5, CreatedAt = new DateTime(2026, 9, 6, 10, 4, 0, DateTimeKind.Utc) }
            );

            await db.SaveChangesAsync();
        });

        // Fixed times in the past, so rows written while a test runs are always newer than these.
        await InsertWithIdentityAsync(db, "Logs", async () =>
        {
            db.Logs.AddRange(
                new Log
                {
                    Id = 1, UserId = 2, AuthorizedRequest = true, Succeeded = true,
                    Action = "PostService.UploadPost", EntityName = "Post", EntityId = 1,
                    CreatedAt = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new Log
                {
                    Id = 2, UserId = 3, AuthorizedRequest = true, Succeeded = false,
                    Action = "PostService.UpdatePostAsync", EntityName = "Post", EntityId = 2,
                    Details = "Caller does not own the post",
                    CreatedAt = new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc)
                },
                new Log
                {
                    Id = 3, UserId = null, AuthorizedRequest = false, Succeeded = false,
                    Action = "AuthService.LoginAsync", EntityName = "User", EntityId = null,
                    CreatedAt = new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc)
                },
                new Log
                {
                    Id = 4, UserId = 2, AuthorizedRequest = true, Succeeded = true,
                    Action = "CommentService.UploadCommentAsync", EntityName = "Comment", EntityId = 1,
                    CreatedAt = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc)
                },
                new Log
                {
                    Id = 5, UserId = null, AuthorizedRequest = false, Succeeded = true,
                    Action = "SoftDeletedUsersCleanupService.ExecuteCycleAsync", EntityName = "User", EntityId = null,
                    CreatedAt = new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc)
                }
            );

            await db.SaveChangesAsync();
        });

        // Fixed past times that rise with the id, so the newest-first order of the report list is known
        // and reports written while a test runs are always the newest.
        await InsertWithIdentityAsync(db, "Reports", async () =>
        {
            db.Reports.AddRange(
                new Report
                {
                    Id = 1, ReporterUserId = 2, TargetType = ReportTargetType.Post, TargetId = 2, Reason = "Alice reports Bob's post.",
                    Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc)
                },
                new Report
                {
                    Id = 2, ReporterUserId = 4, TargetType = ReportTargetType.Comment, TargetId = 1, Reason = "Charlie reports Alice's comment.",
                    Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 9, 7, 10, 1, 0, DateTimeKind.Utc)
                },
                new Report
                {
                    Id = 3, ReporterUserId = 3, TargetType = ReportTargetType.User, TargetId = 4, Reason = "Bob reports Charlie.",
                    Status = ReportStatus.Resolved, ReviewedByUserId = 1,
                    ReviewedAt = new DateTime(2026, 9, 7, 11, 0, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2026, 9, 7, 10, 2, 0, DateTimeKind.Utc)
                },
                // Post 999 never existed: it stands for a target deleted after it was reported.
                new Report
                {
                    Id = 4, ReporterUserId = 5, TargetType = ReportTargetType.Post, TargetId = 999, Reason = "Diana reports a post that is gone.",
                    Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 9, 7, 10, 3, 0, DateTimeKind.Utc)
                },
                // Made by frank_deleted, so the purge of soft-deleted users deletes it.
                new Report
                {
                    Id = 5, ReporterUserId = 7, TargetType = ReportTargetType.Post, TargetId = 3, Reason = "Frank reports Charlie's post.",
                    Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 9, 7, 10, 4, 0, DateTimeKind.Utc)
                },
                // Reviewed by frank_deleted as if he had been an administrator, so the purge clears the reviewer.
                new Report
                {
                    Id = 6, ReporterUserId = 2, TargetType = ReportTargetType.Post, TargetId = 3, Reason = "Alice reports Charlie's post.",
                    Status = ReportStatus.Rejected, ReviewedByUserId = 7,
                    ReviewedAt = new DateTime(2026, 9, 7, 11, 5, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2026, 9, 7, 10, 5, 0, DateTimeKind.Utc)
                }
            );

            await db.SaveChangesAsync();
        });

        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Users', RESEED, 7)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Friendships', RESEED, 5)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Posts', RESEED, 13)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Messages', RESEED, 5)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Comments', RESEED, 6)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('PostLikes', RESEED, 5)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Logs', RESEED, 5)");
        await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Reports', RESEED, 6)");
    }

    private static async Task InsertWithIdentityAsync(ApplicationDbContext db, string tableName, Func<Task> insertAction)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

#pragma warning disable EF1002
        
        await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON");
        await insertAction();
        await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");
        
#pragma warning restore EF1002

        await transaction.CommitAsync();
    }
}

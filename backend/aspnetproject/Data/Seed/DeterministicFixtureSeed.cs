using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Seed;

public static class DeterministicFixtureSeed
{
    private static readonly DateTime SeedNow = new(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime AdultDob = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string PasswordHash = "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=";
    private const string PasswordSalt = "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=";

    public static void SeedDeterministicFixture(this ModelBuilder modelBuilder)
    {
        SeedUsers(modelBuilder);
        SeedFriendships(modelBuilder);
        SeedPosts(modelBuilder);
        SeedComments(modelBuilder);
        SeedMessages(modelBuilder);
        SeedRefreshTokens(modelBuilder);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            ActiveUser(2, "alice", "alice@example.com", "Primary test caller", SeedNow.AddMinutes(-70)),
            ActiveUser(3, "bob", "bob@example.com", "Alice's accepted friend", SeedNow.AddMinutes(-65)),
            ActiveUser(4, "charlie", "charlie@example.com", "Unrelated active user", SeedNow.AddMinutes(-60)),
            ActiveUser(5, "diana", "diana@example.com", "Pending friendship user", SeedNow.AddMinutes(-55)),
            new User
            {
                Id = 6,
                Username = "erin_deactivated",
                Email = "erin@example.com",
                Bio = "Deactivated fixture user",
                DateOfBirth = AdultDob,
                PasswordHash = PasswordHash,
                PasswordSalt = PasswordSalt,
                CreatedAt = SeedNow.AddMinutes(-50),
                AccountDeactivatedAt = SeedNow.AddMinutes(-10),
                ResetTokenHash = "ERINactivationTokenHash000000000000000000",
                ResetTokenExpiresAt = SeedNow.AddDays(1)
            },
            new User
            {
                Id = 7,
                Username = "frank_deleted",
                Email = "frank@example.com",
                Bio = "Soft-deleted fixture user",
                DateOfBirth = AdultDob,
                PasswordHash = PasswordHash,
                PasswordSalt = PasswordSalt,
                CreatedAt = SeedNow.AddMinutes(-45),
                AccountDeletedAt = SeedNow.AddDays(-25)
            });
    }

    private static User ActiveUser(int id, string username, string email, string bio, DateTime createdAt)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = email,
            Bio = bio,
            DateOfBirth = AdultDob,
            PasswordHash = PasswordHash,
            PasswordSalt = PasswordSalt,
            CreatedAt = createdAt
        };
    }

    private static void SeedFriendships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Friendship>().HasData(
            Friendship(1, 2, 3, FriendshipStatus.Accepted, SeedNow.AddMinutes(-40), SeedNow.AddMinutes(-39)),
            Friendship(2, 2, 5, FriendshipStatus.Pending, SeedNow.AddMinutes(-38), null),
            Friendship(3, 4, 2, FriendshipStatus.Pending, SeedNow.AddMinutes(-37), null),
            Friendship(4, 3, 4, FriendshipStatus.Declined, SeedNow.AddMinutes(-36), SeedNow.AddMinutes(-35)));
    }

    private static Friendship Friendship(
        int id,
        int requesterId,
        int addresseeId,
        FriendshipStatus status,
        DateTime sentAt,
        DateTime? lastUpdatedAt)
    {
        return new Friendship
        {
            Id = id,
            RequesterUserId = requesterId,
            AddresseeUserId = addresseeId,
            FriendshipStatus = status,
            SentAt = sentAt,
            CreatedAt = sentAt,
            LastUpdatedAt = lastUpdatedAt
        };
    }

    private static void SeedPosts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>().HasData(
            Post(1, 2, "Alice Post", "Alice owns this post.", SeedNow.AddMinutes(-34)),
            Post(2, 3, "Bob Post", "Bob's post should appear in Alice's feed.", SeedNow.AddMinutes(-33)),
            Post(3, 4, "Charlie Post", "Charlie's public post should not appear in Alice's friend feed.", SeedNow.AddMinutes(-32)),
            Post(4, 6, "Erin Deactivated Post", "Post owned by a deactivated user.", SeedNow.AddMinutes(-31)),
            Post(5, 7, "Frank Deleted Post", "Post owned by a soft-deleted user.", SeedNow.AddMinutes(-30)));
    }

    private static Post Post(int id, int userId, string title, string content, DateTime createdAt)
    {
        return new Post
        {
            Id = id,
            UserId = userId,
            PostTitle = title,
            PostContent = content,
            CreatedAt = createdAt
        };
    }

    private static void SeedComments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>().HasData(
            Comment(1, 2, 2, "Alice comments on Bob's post.", SeedNow.AddMinutes(-29)),
            Comment(2, 3, 2, "Bob comments on his own post.", SeedNow.AddMinutes(-28)),
            Comment(3, 4, 2, "Charlie comments on Bob's post.", SeedNow.AddMinutes(-27)),
            Comment(4, 3, 1, "Bob comments on Alice's post.", SeedNow.AddMinutes(-26)),
            Comment(5, null, 1, "Orphaned comment with no active author.", SeedNow.AddMinutes(-25)));
    }

    private static Comment Comment(int id, int? commenterId, int postId, string content, DateTime createdAt)
    {
        return new Comment
        {
            Id = id,
            CommenterUserId = commenterId,
            PostId = postId,
            CommentContent = content,
            CreatedAt = createdAt
        };
    }

    private static void SeedMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>().HasData(
            Message(1, 3, 2, "Old unread message from Bob to Alice.", false, null, SeedNow.AddMinutes(-24), null),
            Message(2, 2, 3, "Read message from Alice to Bob.", true, SeedNow.AddMinutes(-21), SeedNow.AddMinutes(-22), SeedNow.AddMinutes(-21)),
            Message(3, 2, 3, "Latest message from Alice to Bob.", false, null, SeedNow.AddMinutes(-20), null),
            Message(4, 3, 2, "Latest unread message from Bob to Alice.", false, null, SeedNow.AddMinutes(-19), null));
    }

    private static Message Message(
        int id,
        int senderId,
        int receiverId,
        string content,
        bool seen,
        DateTime? seenAt,
        DateTime createdAt,
        DateTime? lastUpdatedAt)
    {
        return new Message
        {
            Id = id,
            SenderUserId = senderId,
            ReceiverUserId = receiverId,
            MessageContent = content,
            Seen = seen,
            SeenAt = seenAt,
            CreatedAt = createdAt,
            LastUpdatedAt = lastUpdatedAt
        };
    }

    private static void SeedRefreshTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>().HasData(
            RefreshToken(1, 2, "ALICEactiveRefreshTokenHash000000000000000", SeedNow.AddMinutes(-18), SeedNow.AddDays(7), null),
            RefreshToken(2, 2, "ALICErevokedRefreshTokenHash00000000000000", SeedNow.AddMinutes(-17), SeedNow.AddDays(7), SeedNow.AddMinutes(-16)),
            RefreshToken(3, 2, "ALICEexpiredRefreshTokenHash00000000000000", SeedNow.AddDays(-8), SeedNow.AddDays(-1), SeedNow.AddDays(-2)));
    }

    private static RefreshToken RefreshToken(
        int id,
        int userId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? revokedAt)
    {
        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt
        };
    }
}

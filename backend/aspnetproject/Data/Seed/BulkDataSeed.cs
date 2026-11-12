using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using Bogus;

namespace aspnetproject.Data.Seed;

public class BulkDataSeeder
{
    private readonly ApplicationDbContext _db; 
    private readonly Random _rng = new(42);

    public BulkDataSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(int userCount = 300)
    {
        var users = GenerateUsers(userCount);
        _db.Users.AddRange(users);
        await _db.SaveChangesAsync(); // need Ids before FK-dependent inserts

        var friendships = GenerateFriendships(users);
        _db.Friendships.AddRange(friendships);
        await _db.SaveChangesAsync();

        var posts = GeneratePosts(users);
        _db.Posts.AddRange(posts);
        await _db.SaveChangesAsync(); // need post Ids for comments

        var comments = GenerateComments(users, posts);
        _db.Comments.AddRange(comments);

        var messages = GenerateMessages(friendships);
        _db.Messages.AddRange(messages);

        await _db.SaveChangesAsync();
    }

    private List<User> GenerateUsers(int count)
    {
        var faker = new Faker<User>()
            .RuleFor(u => u.Username, (f, u) => f.Internet.UserName().ToLower() + f.Random.Number(1, 9999))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Username))
            .RuleFor(u => u.Bio, f => f.Lorem.Sentence(8))
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(40, DateTime.UtcNow.AddYears(-18)))
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2, DateTime.UtcNow))
            .RuleFor(u => u.LastOnlineAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow))
            .RuleFor(u => u.LastUpdatedAt, f => null)
            .RuleFor(u => u.PasswordHash, f => "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=") // password123
            .RuleFor(u => u.PasswordSalt, f => "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=")
            .RuleFor(u => u.ResetTokenHash, f => "")
            .RuleFor(u => u.ResetTokenExpiresAt, f => null)
            .RuleFor(u => u.UsernameLastChangedAt, f => null);

        return faker.Generate(count);
    }

    private List<Friendship> GenerateFriendships(List<User> users)
    {
        var friendships = new List<Friendship>();
        var existingPairs = new HashSet<(int, int)>();
        var faker = new Faker();

        // target: each user ends up with roughly 5-15 friendship rows (any status)
        foreach (var requester in users)
        {
            var targetCount = _rng.Next(5, 16);

            for (var i = 0; i < targetCount; i++)
            {
                var addressee = users[_rng.Next(users.Count)];
                if (addressee.Id == requester.Id)
                    continue;

                var pairKey = requester.Id < addressee.Id
                    ? (requester.Id, addressee.Id)
                    : (addressee.Id, requester.Id);

                if (existingPairs.Contains(pairKey))
                    continue;

                existingPairs.Add(pairKey);

                var sentAt = faker.Date.Past(1, DateTime.UtcNow);
                var status = faker.Random.WeightedRandom(
                    new[] {FriendshipStatus.Accepted, FriendshipStatus.Pending, FriendshipStatus.Declined},
                    new[] { 0.7f, 0.2f, 0.1f });

                friendships.Add(new Friendship
                {
                    RequesterUserId = requester.Id,
                    AddresseeUserId = addressee.Id,
                    SentAt = sentAt,
                    CreatedAt = sentAt,
                    FriendshipStatus = status,
                    LastUpdatedAt = status == FriendshipStatus.Pending
                        ? null
                        : faker.Date.Between(sentAt, DateTime.UtcNow)
                });
            }
        }

        return friendships;
    }

    private List<Post> GeneratePosts(List<User> users)
    {
        var posts = new List<Post>();
        var faker = new Faker();

        foreach (var user in users)
        {
            var postCount = _rng.Next(0, 11); // 0-10 posts per user

            for (var i = 0; i < postCount; i++)
            {
                var createdAt = faker.Date.Between(user.CreatedAt, DateTime.UtcNow);
                posts.Add(new Post
                {
                    UserId = user.Id,
                    PostTitle = faker.Lorem.Sentence(4).TrimEnd('.'),
                    PostContent = faker.Lorem.Paragraph(2),
                    CreatedAt = createdAt,
                    LastUpdatedAt = faker.Random.Bool(0.2f)
                        ? faker.Date.Between(createdAt, DateTime.UtcNow)
                        : null
                });
            }
        }

        return posts;
    }

    private List<Comment> GenerateComments(List<User> users, List<Post> posts)
    {
        var comments = new List<Comment>();
        var faker = new Faker();

        foreach (var post in posts)
        {
            var commentCount = _rng.Next(0, 6); // 0-5 comments per post

            for (var i = 0; i < commentCount; i++)
            {
                var commenter = users[_rng.Next(users.Count)];
                var createdAt = faker.Date.Between(post.CreatedAt, DateTime.UtcNow);

                comments.Add(new Comment
                {
                    PostId = post.Id,
                    CommenterUserId = commenter.Id,
                    CommentContent = faker.Lorem.Sentence(10),
                    CreatedAt = createdAt,
                    LastUpdatedAt = null
                });
            }
        }

        return comments;
    }

    private List<Message> GenerateMessages(List<Friendship> friendships)
    {
        var messages = new List<Message>();
        var faker = new Faker();

        var acceptedFriendships = friendships.Where(f => f.FriendshipStatus == FriendshipStatus.Accepted).ToList();

        foreach (var friendship in acceptedFriendships)
        {
            if (!faker.Random.Bool(0.6f)) // not every friendship has a message thread
                continue;

            var messageCount = _rng.Next(1, 9); // 1-8 messages per active thread
            var lastTimestamp = friendship.SentAt;

            for (var i = 0; i < messageCount; i++)
            {
                var senderIsRequester = faker.Random.Bool();
                var senderId = senderIsRequester ? friendship.RequesterUserId : friendship.AddresseeUserId;
                var receiverId = senderIsRequester ? friendship.AddresseeUserId : friendship.RequesterUserId;

                lastTimestamp = faker.Date.Between(lastTimestamp, DateTime.UtcNow);
                var seen = faker.Random.Bool(0.75f);

                messages.Add(new Message
                {
                    SenderUserId = senderId,
                    ReceiverUserId = receiverId,
                    MessageContent = faker.Lorem.Sentence(8),
                    CreatedAt = lastTimestamp,
                    LastUpdatedAt = null,
                    Seen = seen,
                    SeenAt = seen ? faker.Date.Between(lastTimestamp, DateTime.UtcNow) : null
                });
            }
        }

        return messages;
    }
}

namespace aspnetproject.Common.ProjectConstants;

/// <summary>
/// The knobs of the feed score, computed in SQL by <c>PostRepository.GetFeedAsync</c>:
/// <code>
/// score = (connection weight + likes + comments) / (age in hours + AgeOffsetHours) ^ AgeGravity
/// </code>
/// <list type="bullet">
/// <item>The connection weight is <see cref="FriendWeight"/> when the author is an accepted friend of the caller,
/// else <see cref="MutualFriendWeight"/> when the author has an accepted friend who is also the caller's friend,
/// else <see cref="OtherUserWeight"/>. A weight counts as that many likes or comments.</item>
/// <item>Age is the minute boundaries crossed between the post's creation and now (so 10:00:59 to 10:01:00 is one
/// minute), divided by 60. A post dated after now counts as zero hours old.</item>
/// <item>The offset keeps a brand-new post from dividing by zero. A higher gravity makes posts sink faster.
/// Every weight is above zero, so a post nobody has reacted to still sinks as it ages.</item>
/// </list>
/// With these values a friend's post nobody has reacted to stays above a brand-new stranger's post for about
/// 12 hours, and a mutual friend's for about 4. They are a first guess, to be tuned once there is real traffic.
/// </summary>
public static class FeedRanking
{
    public const double FriendWeight       = 20;
    public const double MutualFriendWeight = 5;
    public const double OtherUserWeight    = 1;
    public const double AgeOffsetHours     = 2;
    public const double AgeGravity         = 1.5;
}

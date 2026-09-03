namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Common;

public class SoftDeleteCleanupResult
{
    public int UsersDeletedCount { get; set; }
    public int PostsDeletedCount { get; set; }
    public int MessagesDeletedCount { get; set; }
    public int FriendshipsDeletedCount { get; set; }
    
    public static SoftDeleteCleanupResult operator +(SoftDeleteCleanupResult totalResult, SoftDeleteCleanupResult batchResult)
    {
        return new SoftDeleteCleanupResult
        {
            UsersDeletedCount = totalResult.UsersDeletedCount + batchResult.UsersDeletedCount,
            PostsDeletedCount = totalResult.PostsDeletedCount +  batchResult.PostsDeletedCount,
            MessagesDeletedCount = totalResult.MessagesDeletedCount + batchResult.MessagesDeletedCount,
            FriendshipsDeletedCount = totalResult.FriendshipsDeletedCount  + batchResult.FriendshipsDeletedCount
        };
    }
    
}
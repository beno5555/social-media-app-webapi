using System.Collections.Concurrent;

namespace aspnetproject.Infrastructure.Services.Helpers;

public class UserConnectionTracker
{
    private readonly ConcurrentDictionary<int, int> _counts = new();

    public bool AddConnection(int userId)
    {
        int  value    = _counts.AddOrUpdate(userId, 1, (_, count) => count + 1);
        bool firstConnection = value == 1;
        return firstConnection;
    }

    public bool RemoveConnection(int userId)
    {
        bool wasLastConnection = false;
        var  connectionCount = _counts.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));
        if (connectionCount == 0)
        {
            _counts.TryRemove(userId, out _);
            wasLastConnection = true;
        }

        return wasLastConnection;
    }
}
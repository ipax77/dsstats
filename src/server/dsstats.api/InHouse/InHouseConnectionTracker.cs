namespace dsstats.api.InHouse;

public sealed class InHouseConnectionTracker
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, HashSet<string>> userConnections = [];
    private readonly Dictionary<string, Guid> connectionUsers = [];

    public int ConnectedUserCount
    {
        get
        {
            lock (gate)
            {
                return userConnections.Count;
            }
        }
    }

    public int Connect(Guid userId, string connectionId)
    {
        lock (gate)
        {
            connectionUsers[connectionId] = userId;
            if (!userConnections.TryGetValue(userId, out var connections))
            {
                connections = [];
                userConnections[userId] = connections;
            }

            connections.Add(connectionId);
            return userConnections.Count;
        }
    }

    public int Disconnect(string connectionId)
    {
        lock (gate)
        {
            if (!connectionUsers.Remove(connectionId, out var userId))
            {
                return userConnections.Count;
            }

            if (userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    userConnections.Remove(userId);
                }
            }

            return userConnections.Count;
        }
    }
}

using System.Collections.Generic;
using System.Net;

/// <summary>
/// Provides methods to create, obtain and remove UdpConnections
/// </summary>
class UdpConnectionManager
{
    Dictionary<int, UdpConnection> udpConnectionByClientId = new Dictionary<int, UdpConnection>();
    Dictionary<EndPoint, UdpConnection> udpConnectionByEndpoint = new Dictionary<EndPoint, UdpConnection>();
    int nextClientId = 1;
    public Dictionary<int, UdpConnection> Connections
    {
        get
        {
            return udpConnectionByClientId;
        }
    }

    /// <summary>
    /// Returns a connection by endpoint or null if the endpoint is not associated with a connection
    /// </summary>
    internal UdpConnection GetConnection(EndPoint endpoint)
    {
        UdpConnection client;
        if (udpConnectionByEndpoint.TryGetValue(endpoint, out client))
        {
            return client;
        }
        return null;
    }

    /// <summary>
    /// Returns a remote client by client ID or null if the client Id is not associated with a connection
    /// </summary>
    internal UdpConnection GetConnection(ulong clientId)
    {
        UdpConnection client;
        if (udpConnectionByClientId.TryGetValue((int)clientId, out client))
        {
            return client;
        }
        return null;
    }

    internal UdpConnection CreateConnectionObject(bool isServer, EndPoint remoteEndpoint)
    {
        var clientId = isServer ? 0 : GetNextClientId();
        var connection = new UdpConnection(clientId, remoteEndpoint);
        udpConnectionByClientId.Add(clientId, connection);
        udpConnectionByEndpoint[remoteEndpoint] = connection;
        return connection;
    }

    int GetNextClientId()
    {
        return nextClientId++;
    }

    internal void RemoveConnection(UdpConnection connection)
    {
        udpConnectionByClientId.Remove(connection.ClientId);
        udpConnectionByEndpoint.Remove(connection.RemoteEndpoint);
    }
}

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

public class CustomUdpNetworkTransport : NetworkTransport
{
    public override ulong ServerClientId => 0; //the server's client ID is always 0
    UdpConnectionManager connectionManager = new UdpConnectionManager();

    public override void DisconnectLocalClient()
    {
        Debug.Log($"[CustomTransport] DisconnectLocalClient");
        var remoteClient = connectionManager.GetConnection(0);
        remoteClient.Disconnect(); 
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        Debug.Log($"[CustomTransport] DisconnectRemoteClient {clientId}");
        var remoteClient = connectionManager.GetConnection(clientId);
        remoteClient.Disconnect();
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        return 100;
    }

    public override void Initialize()
    {
        Debug.Log("[CustomTransport] Initialize"); 
    }

    ArraySegment<byte> noData = new ArraySegment<byte>();

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        clientId = 0;
        payload = noData;
        receiveTime = Time.realtimeSinceStartup;
        foreach (var clientEntry in connectionManager.Connections)
        {
            var client = clientEntry.Value;
            if (client.State == ClientState.Connected1)
            {
                client.SetState(ClientState.Connected2);
                clientId = (ulong)client.ClientId;
                return NetworkEvent.Connect;
            }

            if (client.State == ClientState.Disconnecting)
            {
                client.SetState(ClientState.Disconnected);
                clientId = (ulong)client.ClientId;
                connectionManager.RemoveConnection(client);
                return NetworkEvent.Disconnect;
            }

            if (client.State == ClientState.Connected2)
            {
                byte[] data = client.Receive();
                if (data != null)
                {
                    payload = new ArraySegment<byte>(data);
                    clientId = (ulong)client.ClientId;
                    return NetworkEvent.Data;
                }
            }
        }

        return NetworkEvent.Nothing;
    }
    

    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
        Debug.Log("[CustomTransport] Send " + payload.Count);
        var client = connectionManager.GetConnection(clientId);
        client.Send(payload);
    }

    public override void Shutdown()
    {
        Debug.Log("[CustomTransport] Shutdown");
        listener?.Close();
    }

    public override bool StartClient()
    {
        Debug.Log("[CustomTransport] StartClient");
        int port = 17890;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        var client = connectionManager.CreateConnectionObject(true, endpoint);
        client.ConnectToServer();
        return true;
    }

    UdpSocket listener;
    public override bool StartServer()
    {
        int listenPort = 17890;
        listener = new UdpSocket();
        listener.OnReceiveFrom += Listener_OnReceiveFrom;
        listener.Listen(new IPEndPoint(IPAddress.Loopback, listenPort));
        Debug.Log($"[CustomTransport] Listening on port {listenPort}");
        return true;
    }

    private void Listener_OnReceiveFrom(EndPoint remoteEndpoint, ArraySegment<byte> data, SocketError status)
    {
        if (status != SocketError.Success)
        {
            return;
        }

        Debug.Log($"[CustomTransport] Data received ({remoteEndpoint})");
        var client = connectionManager.GetConnection(remoteEndpoint);
        if (client == null)
        {
            Debug.Log($"[CustomTransport] data from new source");
            //data received from unknown source, create a new connection object to handle it
            client = connectionManager.CreateConnectionObject(false, remoteEndpoint);
            //the server uses the listener socket to communicate with all clients
            client.SetSocket(listener);
        }
        client.ReceiveData(data, status);
    }
}


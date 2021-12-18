using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
/// <summary>
/// Provides methods to send and receive messages between server and clients
/// A game client will have a single UdpConnection (to the server)
/// The game server will have n UdpConnections (one to each client)
/// The way this object is used differs between client and server
/// </summary>
class UdpConnection
{
    /// <summary>
    /// The Unity netcode clientId associated with the connection
    /// The server has clientId 0
    /// </summary>
    public int ClientId { get; }

    /// <summary>
    /// The endpoint (IP address & port) at the other end of the connection (could be server or client depending on what we are)
    /// </summary>
    public EndPoint RemoteEndpoint { get; }

    /// <summary>
    /// State of the connection
    /// </summary>
    public ClientState State { get; private set; } = ClientState.Initial;

    //underlying socket used for communication
    UdpSocket socket; 
    //received data is accumulated in here until it is read
    List<byte> receivedBytes = new List<byte>();

    /// <summary>
    /// Create a new UDP connection object that will be used to link the given unity netcode clientId with the given remote endpoint
    /// </summary>
    public UdpConnection(int clientId, EndPoint remoteEndpoint)
    {
        ClientId = clientId;
        this.RemoteEndpoint = remoteEndpoint;
    }

    /// <summary>
    /// Connect to server. Only called by clients.
    /// </summary>
    internal void ConnectToServer()
    {
        socket = new UdpSocket();
        socket.Connect(RemoteEndpoint);
        //listen for received data
        socket.OnReceiveFrom += Socket_OnReceiveFrom;
        socket.BeginReceiveFrom();
        //send example handshake. This does nothing other than test a sequence.
        Send(new ArraySegment<byte>(new byte[] { 1, 2 }));
        State = ClientState.ClientHandshake1; 
    }

    /// <summary>
    /// Use an externally provided socket
    /// </summary>
    internal void SetSocket(UdpSocket socket)
    {
        this.socket = socket;
    }

    /// <summary>
    /// Returns the data received since the previous call, or null if there is no received data available
    /// </summary>
    internal byte[] Receive()
    {
        if (State != ClientState.Connected2)
        {
            return null;
        }
        if (receivedBytes.Count > 0)
        {
            var ret = receivedBytes.ToArray();
            receivedBytes.Clear();
            return ret;
        }
        return null;
    }

    /// <summary>
    /// This handler is raised when data is received from the remote endpoint
    /// </summary>
    private void Socket_OnReceiveFrom(EndPoint endpoint, ArraySegment<byte> data, SocketError status)
    {
        //Debug.Log("received from " + endpoint + " + data.Count + " " + status);
        if (status != SocketError.Success)
        {
            Disconnect();
            return;
        }

        receivedBytes.AddRange(data);
        //here the handshake is handled
        if (State == ClientState.Initial)
        {
            if (receivedBytes.Count >= 2)
            {
                Debug.Log("Received handshake1");
                State = ClientState.Connected1;
                receivedBytes.Clear(); //todo just remove first two bytes?
                Send(new ArraySegment<byte>(new byte[] { 1, 2, 3 }));
            }
        }
        if (State == ClientState.ClientHandshake1)
        {
            if (receivedBytes.Count >= 3)
            {
                Debug.Log("Received handshake2");
                State = ClientState.Connected1;
                receivedBytes.Clear(); //todo just remove first three bytes?
            }
        }
    }

    /// <summary>
    /// Send data to the remote endpoint
    /// </summary>
    internal void Send(ArraySegment<byte> payload)
    {
        if (socket.RequiresSendTo)
            socket.SendTo(payload, RemoteEndpoint);
        else
            socket.Send(payload);
    }

    /// <summary>
    /// Feed data into the connection as if it originated from the remote endpoint
    /// </summary>
    internal void ReceiveData(ArraySegment<byte> data, SocketError status)
    {
        Socket_OnReceiveFrom(RemoteEndpoint, data, status);
    }

    internal void SetState(ClientState state)
    {
        State = state;
    }

    internal void Disconnect()
    {
        socket?.Close();
        State = ClientState.Disconnecting; 
    }
}

enum ClientState
{
    Initial,
    ClientHandshake1, //state of client after sending initial connection data and before receiving a confirmation
    Connected1,
    Connected2,
    Disconnecting,
    Disconnected
}

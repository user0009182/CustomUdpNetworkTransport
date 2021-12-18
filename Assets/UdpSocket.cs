using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
/// <summary>
/// Wrapper around Socket for Udp
/// Alternatively UdpClient could have been used
/// </summary>
class UdpSocket
{
    Socket socket;
    EndPoint endpoint;
    //big assumption that packet length will not exceed this
    byte[] buffer = new byte[1000];

    /// <summary>
    /// Raised when data is received
    /// </summary>
    public event Action<EndPoint, ArraySegment<byte>, SocketError> OnReceiveFrom;

    /// <summary>
    /// If true the socket is not bound to a remote endpoint and SendTo must be called to send data
    /// </summary>
    public bool RequiresSendTo { get; private set; }

    internal UdpSocket()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    /// <summary>
    /// Listen on the given endpoint for incoming "connections"
    /// </summary>
    internal void Listen(IPEndPoint endpoint)
    {
        this.endpoint = endpoint;

        //Some sort of magic fix to prevent connection resets being received
        //https://stackoverflow.com/questions/10332630/connection-reset-on-receiving-packet-in-udp-server
        const int SIO_UDP_CONNRESET = -1744830452;
        byte[] inValue = new byte[] { 0 };
        byte[] outValue = new byte[] { 0 };
        socket.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

        socket.Bind(endpoint);
        BeginReceiveFrom();
        RequiresSendTo = true;
    }

    internal void BeginReceiveFrom()
    {
        var args = new SocketAsyncEventArgs();
        args.SetBuffer(buffer, 0, buffer.Length);
        args.RemoteEndPoint = endpoint;
        args.Completed += OnReceiveFromCompleted;
        try
        {
            bool isPending = socket.ReceiveFromAsync(args);
            if (!isPending)
            {
                OnReceiveFromCompleted(this, args);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    private void OnReceiveFromCompleted(object sender, SocketAsyncEventArgs e)
    {
        OnReceiveFrom(e.RemoteEndPoint, new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred), e.SocketError);
        BeginReceiveFrom();
    }

    internal void Connect(EndPoint endpoint)
    {
        this.endpoint = endpoint;
        socket.Connect(endpoint);
        RequiresSendTo = false;
    }

    internal int Send(ArraySegment<byte> payload)
    {
        return socket.Send(payload.Array, payload.Offset, payload.Count, SocketFlags.None);
    }

    internal int SendTo(ArraySegment<byte> payload, EndPoint endpoint)
    {
        return socket.SendTo(payload.Array, payload.Offset, payload.Count, SocketFlags.None, endpoint);
    }

    internal void Close()
    {
        socket?.Close();
    }
}
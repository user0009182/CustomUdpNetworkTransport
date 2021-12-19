# CustomUdpNetworkTransport

This is a custom Netcode for GameObjects transport that uses UDP using C# Sockets for educational purposes. Unity Netcode for GameObjects (https://docs-multiplayer.unity3d.com/docs/getting-started/about) is the new multiplayer component for Unity. This transport shouldn't be used in a game, it is neither complete or robust.

There's an example game/application in the project that is set up to use the transport. Press Start Host to host a game, then in a separate instance of the game press Start Client to connect to that host. The example allows each player to move using WASD and their character color (C key).

Below are a set of notes to explain how custom transports can be implemented. Some of the detail is guesswork and may be incorrect.

# Quick Overview

Netcode for GameObjects decouples the high level multiplayer API from the underlying network/transport protocol. This allows different transports to be plugged in, without requiring code changes at the game level. The default transport is Unity's UNet, which uses UDP and there are others available (https://github.com/Unity-Technologies/multiplayer-community-contributions). For example WebSockets uses browser based websockets instead of UDP.

A new transport is implemented by deriving from the abstract class Unity.Netcode.NetworkTransport. The derived class can then be plugged into the NetworkManager and netcode will use the new transport.

Unity.Netcode.NetworkTransport defines 8 methods and one property that must be overridden/implemented. Creating a new custom transport is pretty much just a case of implementing these methods and property.

# The 8 methods (and 1 property)

**public abstract ulong ServerClientId**

Each connected client has a unique client Id. The server has a unique client Id too. Yes the game server has a ClientId. When a client wants to send a message to the server, it specifes the server's client Id. It's important to grasp that the "clients" referred to by clientIds are not the same as game clients. You can think of them as destination IDs. This ServerClientId property should return the client Id of the server. The transport determines how client Ids are assigned, including what the server's clientId is.

**public abstract void Initialize()**

Called first. The Transport can do any initialisation it wants here.

**public abstract void Shutdown()**

Called last. The Transport can clean up resources here.

**public abstract bool StartClient()**

The Transport should implement StartClient by opening a connection to the server. It shouldn't block waiting though (none of these methods should block or delay), it should return true to indicate that the effort to connect is being made, or false if it can't even try.

**public abstract bool StartServer();**

The transport should implement StartServer by beginning to accept connections from clients. For example open a listening socket. If this is successful it should return true.

**public abstract void Send(ulong clientId, ArraySegment\<byte\> payload, NetworkDelivery networkDelivery);**
  
When Send is called, the transport should send the given data/payload to the destination associated with the given clientId (remember this could be the server). NetworkDelivery contains reliability options. I haven't looked into that yet.

**public abstract void DisconnectRemoteClient(ulong clientId);**

End the connection to the given destination.
  
**public abstract void DisconnectLocalClient();**

End the connection to the server (only called in the context of the client?)
  
**public abstract ulong GetCurrentRtt(ulong clientId);**

Return the round-trip time. Haven't looked into what this is for, I think I saw it was optional. Perhaps this is used for a calculation that requires latency.
  
**public abstract NetworkEvent PollEvent(out ulong clientId, out ArraySegment<\byte> payload, out float receiveTime);**
  
This method is central to how the NetworkManager communicates with the transport. The NetworkManager calls this PollEvent method rapidly (something like every network frame), asking the transport if anything has happened. The return type NetworkEvent is an enum with 4 values, but notice there are also 3 output parameters that are effectively returned too.
  
These are the possible NetworkEvent values:
Data
Connect
Disconnect
Nothing

* If nothing has happened since the last poll, then NetworkEvent.Nothing should be returned.
* If a client has connected then NetworkEvent.Connect should be returned and the clientId output parameter should be set to the Id of the new client. More on clientIds below.
* If a client has disconnected then NetworkEvent.Disconnect should be returned and the clientId output parameter should be set to the Id of the disconnecting client.
* If data has been received from a client, then NetworkEvent.Data should be returned, the clientId output parameter set to the Id of the client that received the data, and the payload output parameter set to hold the data itself.
* (The out parameter receiveTime should be set to Time.realtimeSinceStartup)
  
**Call order**
  
A quick mention of the kind of order to expect the above methods to be called in.
  
Just a reminder of the 3 possible ways to start multiplayer with NetworkManager:
NetworkManager.Singleton.StartClient()   run as a client connecting to a server
NetworkManager.Singleton.StartServer()   run as a standalone server that other clients will connect to
NetworkManager.Singleton.StartHost()     run both a server and a client (self-host the game)  

If self-hosting, this is the order of calls into the transport. (PollEvent is called many times, only the relevant calls listed below):  

Initialize()
StartServer()
//NetworkManager.Singleton.OnClientConnectedCallback is raised indicating the host client was connected to the server. This has nothing to do with the transport though.
//silence until a client connects
//When a client does connect, a subsequent PollEvent should return NetworkEvent.Connect with a unique clientId for the client
PollEvent() returning NetworkEvent.Connect
//The NetworkManager waits for the client to send data.
//Data is received from the client, a subsequent PollEvent should return NetworkEvent.Data with the clientId of the client
//The NetworkManager now sends packets to the client
//Conversation continues
  
If connecting to a server
Initialize()
StartClient()
//once the client knows the connection is successful, a subsequent PollEvent should return NetworkEvent.Connect with the clientId of the server
//The NetworkManager sees the NetworkEvent.Connect and sends a packet to the server.
Send(bytes)
//The server sends back some packets. A subsequent PollEvent should return NetworkEvent.Data for each packet. 
//At this point NetworkManager.Singleton.OnClientConnectedCallback is raised
//Conversation continues

**Additional Nodes**
* On a connection, both sides must raise a PollEvent returning NetworkEvent.Connect. Not doing this on either side will cause traffic to freeze.
* Everything hinges on data being sent to the correct destination for a given clientId. Chaos will happen if clientIds are not unique or consistent.
* It's the responsibility of the transport to detect timeouts and return Disconnect from a PollEvent.

  
  

# CustomUdpNetworkTransport
An example of a custom transport for Unity Netcode for GameObjects that uses the C# Socket class. The main implementation is in CustomUdpNetworkTransport.cs which inherits from the Unity NetworkTransport.

The project contains an example game that is set up to use the transport. Press Start Host to host a game, then in a separate instance of the game press Start Client to connect to that host. The example allows each player to move using WASD and their character color (C).

I'm sharing this to help anyone out who wants to create their own custom transport, or wants to see how the NetworkTransport interface works. I'll post some additional information here once I figure out more about the methods and call orders.

Don't use this in an actual game! It's not complete and isn't robust enough.

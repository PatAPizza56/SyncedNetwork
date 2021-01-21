using System;
using System.Collections.Generic;
using SyncedNetwork.Server;

class DemoServer
{
    // Server and Messages objects needed for running the server and sending messages
    static Server server = new Server();
    static Dictionary<int, Message> messages = new Dictionary<int, Message>();

    // Constant variables for starting the Server
    static int Port = 10707;
    static int MaxPlayers = 2;

    public static void Main(string[] args)
    {
        #region "Message"

        messages.Add(1, // Message ID
            new Message()
            {
                Strings = new string[] { "(Welcome)" } // Set up the amount information that will be sent using empty strings or 0's. You can change the values later
            });

        messages.Add(2, // Message ID
            new Message()
            {
                Strings = new string[] { "(Player joined)" } // Set up the amount information that will be sent using empty strings or 0's. You can change the values later
            });

        messages.Add(3, // Message ID
            new Message()
            {
                Strings = new string[] { "(Player left)" } // Set up the amount information that will be sent using empty strings or 0's. You can change the values later
            });

        #endregion

        server.StartServer(Port, MaxPlayers, messages, OnRecieveMessage, OnClientConnect, OnClientDisconnect); // Start the server. The last 2 Callbacks are optional
    }

    #region "Recieve Message Callback"

    static void OnRecieveMessage(int clientID, int packetID, Message message)
    {
        switch (packetID) // The information that the Message object contains is based on what the client sent, not based on the corresponding Message ID in the server
        {
            case 1: // This is a "Thank you" packet. It is sent when the welcome message is recieved
                {
                    Console.WriteLine($"Client: {clientID} sent a message: {message.Strings[0]}");

                    break;
                }
            case 2: // This is an "Okay" packet. It is sent when the connect and disconnect messages are recieved
                {
                    Console.WriteLine($"Client: {clientID} sent a message: {message.Strings[0]}");

                    break;
                }
        }
    }

    #endregion

    #region "Connect and Disconnect Callbacks

    static void OnClientConnect(int clientID)
    { 
        Console.WriteLine($"{clientID} connected to the server");
        
        // For each client, send a message (If a client spot is not filled, the server will automatically skip that client)
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (i != clientID) // If this is not the newly joined client
            {
                messages[2].Strings[0] = $"Player with an ID of: {clientID} has joined"; // Set the text that the message should send
                server.SendMessage(i, 2); // Send "Player has joined" message
            }
            else // If this is the newly joined client
            {
                messages[1].Strings[0] = "Welcome to the server!"; // Set the text that the message should send
                server.SendMessage(clientID, 1); // Send "Welcome" message
            }
        }
    }

    static void OnClientDisconnect(int clientID)
    { 
        Console.WriteLine($"{clientID} disconnected to the server");

        // For each client, send a message (If a client spot is not filled, the server will automatically skip that client)
        for (int i = 0; i < MaxPlayers; i++)
        {
            messages[3].Strings[0] = $"Player with an ID of: {clientID} has left"; // Set the text that the message should send
            server.SendMessage(i, 3); // Send "Player has left" message
        }
    }

    #endregion
}
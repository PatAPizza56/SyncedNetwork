using System;
using System.Collections.Generic;
using SyncedNetwork.Client;

class DemoClient
{
    // Client and Messages objects needed for running the client and sending messages
    static Client client = new Client();
    static Dictionary<int, Message> messages = new Dictionary<int, Message>();

    // Constant variables for starting the Server
    static string IP = "127.0.0.1";
    static int Port = 50707;

    public static void Main(string[] args)
    {
        #region "Messages"

        messages.Add(1, // Message ID
            new Message()
            {
                Strings = new string[] { "(Thank you)" } // Set up the amount information that will be sent using empty strings or 0's. You can change the values later
            });

        messages.Add(2, // Message ID
            new Message()
            {
                Strings = new string[] { "(Okay)" } // Set up the amount information that will be sent using empty strings or 0's. You can change the values later
            });

        #endregion

        client.ConnectToServer(IP, Port, messages, OnRecieveMessage, OnConnectToServerSuccess, OnConnectToServerFailed, OnDisconnectedFromServer); // Connect to the server. The last 3 Callbacks are optional
    }

    #region "Recieve Message Callback"

    static void OnRecieveMessage(int packetID, Message message)
    {
        switch (packetID) // The information that the Message object contains is based on what the server sent, not based on the corresponding Message ID in the client
        {
            case 1: // This is a "Welcome" packet. It is sent when the client joins the server
                {
                    Console.WriteLine(message.Strings[0]);

                    messages[1].Strings[0] = "Thank you!";
                    client.SendMessage(1);

                    break;
                }
            case 2: // This is a "Player Joined" packet. It is sent when a player connects to the server
                {
                    Console.WriteLine(message.Strings[0]);

                    messages[2].Strings[0] = "Okay!";
                    client.SendMessage(2);

                    break;
                }
            case 3: // This is a "Player Joined" packet. It is sent when a player disconnects from the server
                {
                    Console.WriteLine(message.Strings[0]);

                    messages[2].Strings[0] = "Okay!";
                    client.SendMessage(2);

                    break;
                }
        }
    }

    #endregion

    #region "Connect and Disconnect Callbacks"

    static void OnConnectToServerSuccess(string message) { Console.WriteLine(message); }
    static void OnConnectToServerFailed(string message) { Console.WriteLine(message); }
    static void OnDisconnectedFromServer(string message) { Console.WriteLine(message); }

    #endregion
}
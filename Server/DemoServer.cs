using System;
using System.Collections.Generic;
using SyncedNetwork.Server;

class DemoServer
{
    static Server server;
    static Dictionary<int, Message> messages;

    public static void Main(string[] args)
    {
        server = new Server();
        messages = new Dictionary<int, Message>();

        messages.Add(1, new Message()
        {
            Strings = new string[] { "Hello server, this is PatAPizza!" },
            Integers = new int[] { 10, 7 }
        });
        messages.Add(2, new Message()
        {
            Strings = new string[] { "Hello PatAPizza, this is the server!" },
            Integers = new int[] { 7, 10 }
        });

        server.SetMessages(messages);
        server.OnRecieveMessage(OnRecieveMessage);

        server.StartServer(10707);
    }

    static void OnRecieveMessage(int clientID, int packetID, Message message)
    {
        switch (packetID)
        {
            case 1:
                {
                    Console.WriteLine($"Packet ID: ${packetID}");

                    Console.WriteLine(message.Strings[0]);
                    Console.WriteLine(message.Integers[0]);
                    Console.WriteLine(message.Integers[1]);

                    server.SendMessage(clientID, 2);

                    break;
                }
            case 2:
                {
                    Console.WriteLine($"Packet ID: ${packetID}");

                    Console.WriteLine(message.Strings[0]);
                    Console.WriteLine(message.Integers[1]);
                    Console.WriteLine(message.Integers[0]);

                    break;
                }
        }
    }
}
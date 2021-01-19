using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SyncNetworking.Client;

class DemoClient
{
    static Client client;
    static Dictionary<int, Message> messages;

    public static void Main(string[] args)
    {
        client = new Client();
        messages = new Dictionary<int, Message>();

        messages.Add(1, new Message()
        {
            Strings = new string[] { "Hello server, this is PatAPizza!" },
            Integers = new int[] { 10, 7 },
        });
        messages.Add(2, new Message()
        {
            Strings = new string[] { "Hello PatAPizza, this is the server!" },
            Integers = new int[] { 7, 10 }
        });

        client.SetMessages(messages);
        client.OnRecieveMessage(OnRecieveMessage);

        client.ConnectToServer("127.0.0.1", 10707);

        client.SendMessage(1);
    }

    static void OnRecieveMessage(int packetID, Message message)
    {
        switch (packetID)
        {
            case 1:
                {
                    Console.WriteLine($"Packet ID: ${packetID}");

                    Console.WriteLine(message.Strings[0]);
                    Console.WriteLine(message.Integers[0]);
                    Console.WriteLine(message.Integers[1]);

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
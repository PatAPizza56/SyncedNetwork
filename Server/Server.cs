using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SyncNetworking.Server
{
    public class Server
    {
        Thread threadConsole;

        ServerHandle handle;
        ServerTCP TCP;

        Dictionary<int, Message> messages;
        RecieveMessageCallback onRecieveMessage;
        public delegate void RecieveMessageCallback(int clientID, int packetID, Message message);

        public void StartServer(int port)
        {
            threadConsole = new Thread(new ThreadStart(ConsoleThread));
            threadConsole.Start();

            handle = new ServerHandle();
            
            handle.SetMessages(messages, onRecieveMessage);

            TCP = new ServerTCP();

            for (int i = 0; i < ServerTCP.maxPlayers; i++)
            {
                TCP.clients[i] = new ServerClient(handle);
            }

            handle.SetTCP(TCP);

            TCP.StartServer(port);
        }

        public void SetMessages(Dictionary<int, Message> messages)
        {
            this.messages = messages;
        }

        public void SendMessage(int clientID, int packetID)
        {
            ByteBuffer buffer = new ByteBuffer();

            buffer.Write(packetID);

            Message message;

            if (messages.TryGetValue(packetID, out message)) { }
            else { return; }

            if (message.Integers != null)
            {
                for (int i = 0; i < message.Integers.Length; i++)
                {
                    buffer.Write(message.Integers[i]);
                }
            }
            if (message.Floats != null)
            {
                for (int i = 0; i < message.Floats.Length; i++)
                {
                    buffer.Write(message.Floats[i]);
                }
            }
            if (message.Shorts != null)
            {
                for (int i = 0; i < message.Shorts.Length; i++)
                {
                    buffer.Write(message.Shorts[i]);
                }
            }
            if (message.Longs != null)
            {
                for (int i = 0; i < message.Longs.Length; i++)
                {
                    buffer.Write(message.Longs[i]);
                }
            }
            if (message.Strings != null)
            {
                for (int i = 0; i < message.Strings.Length; i++)
                {
                    buffer.Write(message.Strings[i]);
                }
            }

            TCP.SendDataTo(clientID, buffer.ToArray());

            buffer.Dispose();
        }

        public void OnRecieveMessage(RecieveMessageCallback onRecieveMessage)
        {
            this.onRecieveMessage = onRecieveMessage;
        }

        void ConsoleThread() { while (true) { } }
    }

    public class ServerTCP
    {
        public const int maxPlayers = 2;
        public ServerClient[] clients = new ServerClient[maxPlayers];
        
        TcpListener serverSocket;

        public void StartServer(int port)
        {
            serverSocket = new TcpListener(IPAddress.Any, port);
            
            //serverSocket.Server.NoDelay = true;
            
            serverSocket.Start();

            serverSocket.BeginAcceptTcpClient(OnClientConnect, null);
        }

        #region "MAKE SURE TO ADD CALLBACKS TO SERVER SCRIPT AND CLIENT SCRIPT"

        void OnClientConnect(IAsyncResult result)
        {
            TcpClient client = serverSocket.EndAcceptTcpClient(result);

            Console.WriteLine("Connection recieved from {0}", client.Client.RemoteEndPoint.ToString());

            serverSocket.BeginAcceptTcpClient(OnClientConnect, null);

            for (int i = 0; i < maxPlayers; i++)
            {
                if (clients[i].socket == null)
                {
                    clients[i].socket = client;
                    clients[i].ID = i;
                    clients[i].IP = client.Client.RemoteEndPoint.ToString();
                    
                    clients[i].InitializeClient();

                    Console.WriteLine("Connection accepted from {0}", client.Client.RemoteEndPoint.ToString());

                    return;
                }
            }
        }

        #endregion

        public void SendDataTo(int clientID, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();

            buffer.Write((data.GetUpperBound(0) - data.GetLowerBound(0)) + 1);
            buffer.Write(data);

            clients[clientID].stream.BeginWrite(buffer.ToArray(), 0, buffer.Count(), null, null);
        }
    }

    public class ServerHandle
    {
        ServerTCP tcp;

        int packetLength;
        Dictionary<int, Message> messages;
        Server.RecieveMessageCallback onRecieveMessage;

        public void SetTCP(ServerTCP tcp)
        {
            this.tcp = tcp;
        }

        public void SetMessages(Dictionary<int, Message> messages, Server.RecieveMessageCallback onRecieveMessage)
        {
            this.messages = messages;
            this.onRecieveMessage = onRecieveMessage;
        }

        public void HandleData(int clientID, byte[] data)
        {
            byte[] buffer;
            buffer = (byte[])data.Clone();

            ByteBuffer clientBuffer;

            if (tcp.clients[clientID].buffer == null) { tcp.clients[clientID].buffer = new ByteBuffer(); }

            clientBuffer = tcp.clients[clientID].buffer;

            clientBuffer.Write(buffer);

            if (clientBuffer.Count() == 0)
            {
                clientBuffer.Clear();

                return;
            }

            if (clientBuffer.Length() >= 4)
            {
                packetLength = clientBuffer.ReadInt(false);

                if (packetLength <= 0)
                {
                    clientBuffer.Clear();

                    return;
                }
            }

            while (packetLength > 0 && packetLength <= clientBuffer.Length() - 4)
            {
                if (packetLength <= clientBuffer.Length() - 4)
                {
                    clientBuffer.ReadInt();
                    data = clientBuffer.ReadBytes(packetLength);

                    HandleDataPackets(clientID, data);
                }

                packetLength = 0;

                if (clientBuffer.Length() >= 4)
                {
                    packetLength = clientBuffer.ReadInt(false);

                    if (packetLength < 0)
                    {
                        clientBuffer.Clear();

                        return;
                    }
                }
            }
        }

        void HandleDataPackets(int clientID, byte[] data)
        {
            int packetID;
            Message message;

            ByteBuffer buffer = new ByteBuffer();

            buffer.Write(data);
            packetID = buffer.ReadInt();

            if (messages.TryGetValue(packetID, out message))
            {
                if (message.Integers != null)
                {
                    List<int> integers = new List<int>();

                    for (int i = 0; i < message.Integers.Length; i++)
                    {
                        integers.Add(buffer.ReadInt());
                    }

                    message.Integers = integers.ToArray();
                }
                if (message.Floats != null)
                {
                    List<float> floats = new List<float>();

                    for (int i = 0; i < message.Floats.Length; i++)
                    {
                        floats.Add(buffer.ReadFloat());
                    }

                    message.Floats = floats.ToArray();
                }
                if (message.Shorts != null)
                {
                    List<short> shorts = new List<short>();

                    for (int i = 0; i < message.Shorts.Length; i++)
                    {
                        shorts.Add(buffer.ReadShort());
                    }

                    message.Shorts = shorts.ToArray();
                }
                if (message.Longs != null)
                {
                    List<long> longs = new List<long>();

                    for (int i = 0; i < message.Longs.Length; i++)
                    {
                        longs.Add(buffer.ReadLong());
                    }

                    message.Longs = longs.ToArray();
                }
                if (message.Strings != null)
                {
                    List<string> strings = new List<string>();

                    for (int i = 0; i < message.Strings.Length; i++)
                    {
                        strings.Add(buffer.ReadString());
                    }

                    message.Strings = strings.ToArray();
                }

                onRecieveMessage(clientID, packetID, message);
            }
            else { return; }

            buffer.Dispose();
        }
    }

    public class Message
    {
        public int[] Integers = null;
        public float[] Floats = null;
        public short[] Shorts = null;
        public long[] Longs = null;
        public string[] Strings = null;
    }
}
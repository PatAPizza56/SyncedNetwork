using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SyncedNetwork.Server
{
    public class Server
    {
        Thread threadConsole;

        ServerHandle handle;
        ServerTCP TCP;

        Dictionary<int, Message> messages;

        public delegate void RecieveMessageCallback(int clientID, int packetID, Message message);
        public delegate void OnClientConnect(int clientID);
        public delegate void OnClientDisconnect(int clientID);

        public void StartServer(int port, int maxPlayers, Dictionary<int, Message> messages, RecieveMessageCallback onRecieveMessage)
        {
            try
            {
                this.messages = messages;

                threadConsole = new Thread(new ThreadStart(ConsoleThread));
                threadConsole.Start();

                TCP = new ServerTCP(port, maxPlayers);
                handle = new ServerHandle(TCP, messages, onRecieveMessage);

                for (int i = 0; i < TCP.maxPlayers; i++)
                {
                    TCP.clients[i] = new ServerClient(handle);
                }

                TCP.StartServer();
            }
            catch (Exception reason)
            {
                throw new Exception($"Failed to start server, reason: {reason}");
            }
        }
        public void StartServer(int port, int maxPlayers, Dictionary<int, Message> messages, RecieveMessageCallback onRecieveMessage, OnClientConnect onClientConnect)
        {
            try
            {
                this.messages = messages;

                threadConsole = new Thread(new ThreadStart(ConsoleThread));
                threadConsole.Start();

                TCP = new ServerTCP(port, maxPlayers, onClientConnect);
                handle = new ServerHandle(TCP, messages, onRecieveMessage);

                for (int i = 0; i < TCP.maxPlayers; i++)
                {
                    TCP.clients[i] = new ServerClient(handle);
                }

                TCP.StartServer();
            }
            catch (Exception reason)
            {
                throw new Exception($"Failed to start server, reason: {reason}");
            }
        }
        public void StartServer(int port, int maxPlayers, Dictionary<int, Message> messages, RecieveMessageCallback onRecieveMessage, OnClientConnect onClientConnect, OnClientDisconnect onClientDisconnect)
        {
            try
            {
                this.messages = messages;

                threadConsole = new Thread(new ThreadStart(ConsoleThread));
                threadConsole.Start();

                TCP = new ServerTCP(port, maxPlayers, onClientConnect);
                handle = new ServerHandle(TCP, messages, onRecieveMessage);

                for (int i = 0; i < TCP.maxPlayers; i++)
                {
                    TCP.clients[i] = new ServerClient(handle, onClientDisconnect);
                }

                TCP.StartServer();
            }
            catch (Exception reason)
            {
                throw new Exception($"Failed to start server, reason: {reason}");
            }
        }

        public void SendMessage(int clientID, int packetID)
        {
            try
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
            catch (Exception reason)
            {
                throw new Exception($"Failed to send message, reason: {reason}");
            }
        }

        void ConsoleThread() { while (true) { } }
    }

    public class ServerTCP
    {
        public int maxPlayers;
        public ServerClient[] clients;

        Server.OnClientConnect onClientConnect;

        int port;
        TcpListener serverSocket;

        public ServerTCP(int port, int maxPlayers)
        {
            this.port = port;

            this.maxPlayers = maxPlayers;
            this.clients = new ServerClient[this.maxPlayers];
        }
        public ServerTCP(int port, int maxPlayers, Server.OnClientConnect onClientConnect)
        {
            this.port = port;

            this.maxPlayers = maxPlayers;
            this.clients = new ServerClient[this.maxPlayers];

            this.onClientConnect = onClientConnect;
        }

        public void StartServer()
        {
            serverSocket = new TcpListener(IPAddress.Any, port);            
            serverSocket.Start();

            serverSocket.BeginAcceptTcpClient(OnClientConnect, null);
        }

        void OnClientConnect(IAsyncResult result)
        {
            TcpClient client = serverSocket.EndAcceptTcpClient(result);

            serverSocket.BeginAcceptTcpClient(OnClientConnect, null);

            for (int i = 0; i < maxPlayers; i++)
            {
                if (clients[i].socket == null)
                {
                    clients[i].socket = client;
                    clients[i].ID = i;
                    clients[i].IP = client.Client.RemoteEndPoint.ToString();
                    
                    clients[i].InitializeClient();

                    if (onClientConnect != null) { onClientConnect(clients[i].ID); }

                    return;
                }
            }
        }

        public void SendDataTo(int clientID, byte[] data)
        {
            if (clients[clientID].stream == null) { return; }

            ByteBuffer buffer = new ByteBuffer();

            buffer.Write((data.GetUpperBound(0) - data.GetLowerBound(0)) + 1);
            buffer.Write(data);

            clients[clientID].stream.BeginWrite(buffer.ToArray(), 0, buffer.Count(), null, null);
        }
    }

    public class ServerHandle
    {
        ServerTCP TCP;

        int packetLength;
        Dictionary<int, Message> messages;
        Server.RecieveMessageCallback onRecieveMessage;

        public ServerHandle(ServerTCP TCP, Dictionary<int, Message> messages, Server.RecieveMessageCallback onRecieveMessage)
        {
            this.TCP = TCP;
            this.messages = messages;
            this.onRecieveMessage = onRecieveMessage;
        }

        public void HandleData(int clientID, byte[] data)
        {
            byte[] buffer;
            buffer = (byte[])data.Clone();

            ByteBuffer clientBuffer;

            if (TCP.clients[clientID].buffer == null) { TCP.clients[clientID].buffer = new ByteBuffer(); }

            clientBuffer = TCP.clients[clientID].buffer;

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

                    HandleMessages(clientID, data);
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

        void HandleMessages(int clientID, byte[] data)
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
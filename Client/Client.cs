using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SyncNetworking.Client
{
    public class Client
    {
        Thread threadConsole;

        ClientHandle handle;
        ClientTCP TCP;

        Dictionary<int, Message> messages;
        RecieveMessageCallback onRecieveMessage;
        public delegate void RecieveMessageCallback(int packetID, Message message);

        public void ConnectToServer(string IP, int port)
        {
            // MORE CALLBACKS!!
            // On sendmessage success, on sendmessagefailed
            // on connect success, on connect failed

            threadConsole = new Thread(new ThreadStart(ConsoleThread));
            threadConsole.Start();

            handle = new ClientHandle();
            handle.SetMessages(messages, onRecieveMessage);

            TCP = new ClientTCP(handle);
            TCP.ConnectToServer(IP, port);
        }

        public void SetMessages(Dictionary<int, Message> messages)
        {
            this.messages = messages;
        }

        public void SendMessage(int packetID)
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

            TCP.SendMessage(buffer.ToArray());

            buffer.Dispose();
        }

        public void OnRecieveMessage(RecieveMessageCallback onRecieveMessage)
        {
            this.onRecieveMessage = onRecieveMessage;
        }

        public void DisconnectFromServer()
        {

        }

        void ConsoleThread() { while (true) { } }
    }

    public class ClientTCP
    {
        public ClientHandle handle;

        TcpClient socket;
        NetworkStream stream;

        int bufferSize;
        int asyncBufferSize;
        byte[] asyncBuffer;

        public ClientTCP(ClientHandle handle)
        {
            this.handle = handle;
        }

        public void ConnectToServer(string IP, int port, int bufferSize = 4096)
        {
            this.bufferSize = bufferSize;
            this.asyncBufferSize = this.bufferSize + this.bufferSize;
            asyncBuffer = new byte[asyncBufferSize];

            socket = new TcpClient();
            socket.ReceiveBufferSize = this.bufferSize;
            socket.SendBufferSize = this.bufferSize;

            socket.BeginConnect(IP, port, new AsyncCallback(OnConnectedToServer), socket);
        }

        #region "Callbacks"

        void OnConnectedToServer(IAsyncResult result)
        {
            try
            {
                socket.EndConnect(result);

                if (socket.Connected == false)
                {
                    OnConnectToServerFailed();

                    return;
                }
                else
                {
                    OnConnectToServerSuccess();

                    stream = socket.GetStream();
                    stream.BeginRead(asyncBuffer, 0, asyncBufferSize, OnRecieveData, null);
                }
            }
            catch (Exception exception)
            {
                OnConnectToServerFailed(exception);
            }
        }
        void OnConnectToServerSuccess()
        {
            Console.WriteLine("Succesfully connected to server");
        }
        void OnConnectToServerFailed()
        {
            Console.WriteLine("Failed to connect to server");
        }
        void OnConnectToServerFailed(Exception exception)
        {
            Console.WriteLine($"Failed to connect to server, error: {exception}");
        }

        void OnRecieveData(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                byte[] data = new byte[byteLength];

                Buffer.BlockCopy(asyncBuffer, 0, data, 0, byteLength);

                if (byteLength == 0)
                {
                    OnDisconnectFromServer();

                    return;
                }

                handle.HandleData(data);

                stream.BeginRead(asyncBuffer, 0, asyncBufferSize, OnRecieveData, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                OnRecieveDataFailed();
            }
        }

        void OnRecieveDataFailed()
        {
            Console.WriteLine("Failed to recieve data");
        }

        void OnDisconnectFromServer()
        {
            Console.WriteLine("Disconnected from server");
        }

        #endregion

        public void SendMessage(byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer();

            buffer.Write((data.GetUpperBound(0) - data.GetLowerBound(0) + 1));
            buffer.Write(data);

            //Figure out this error: Object reference not set to an instance of an object (stream was null)
            stream.Write(buffer.ToArray(), 0, buffer.ToArray().Length);

            buffer.Dispose();
        }
    }

    public class ClientHandle
    {
        ByteBuffer buffer;
        
        int packetLength;
        Dictionary<int, Message> messages;
        Client.RecieveMessageCallback onRecieveMessage;
        
        public void SetMessages(Dictionary<int, Message> messages, Client.RecieveMessageCallback onRecieveMessage)
        {
            Console.WriteLine("Setting messages");

            this.messages = messages;
            this.onRecieveMessage = onRecieveMessage;
        }

        public void HandleData(byte[] data)
        {
            byte[] buffer;
            buffer = (byte[])data.Clone();

            if (this.buffer == null)
            {
                this.buffer = new ByteBuffer();
            }

            this.buffer.Write(buffer);

            if (this.buffer.Count() == 0)
            {
                this.buffer.Clear();

                return;
            }

            if (this.buffer.Length() >= 4)
            {
                packetLength = this.buffer.ReadInt(false);

                if (packetLength <= 0)
                {
                    this.buffer.Clear();

                    return;
                }
            }

            while (packetLength > 0 && packetLength <= this.buffer.Length() - 4)
            {
                if (packetLength <= this.buffer.Length() - 4)
                {
                    this.buffer.ReadInt();
                    data = this.buffer.ReadBytes(packetLength);

                    HandleDataMessages(data);
                }

                packetLength = 0;

                if (this.buffer.Length() >= 4)
                {
                    packetLength = this.buffer.ReadInt(false);

                    if (packetLength < 0)
                    {
                        this.buffer.Clear();

                        return;
                    }
                }
            }
        }

        void HandleDataMessages(byte[] data)
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

                onRecieveMessage(packetID, message); 
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
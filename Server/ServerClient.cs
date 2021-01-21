using System;
using System.Net.Sockets;

namespace SyncedNetwork.Server
{
    public class ServerClient
    {
        public int ID;
        public string IP;
        public TcpClient socket;
        public NetworkStream stream;
        public ByteBuffer buffer;

        ServerHandle handle;
        Server.OnClientDisconnect onClientDisconnect;

        int bufferSize = 4096;
        byte[] readBuffer;

        public ServerClient(ServerHandle handle)
        {
            this.handle = handle;
            this.onClientDisconnect = null;
        }
        public ServerClient(ServerHandle handle, Server.OnClientDisconnect onClientDisconnect)
        {
            this.handle = handle;
            this.onClientDisconnect = onClientDisconnect;
        }

        public void InitializeClient()
        {
            socket.SendBufferSize = bufferSize;
            socket.ReceiveBufferSize = bufferSize;

            readBuffer = new byte[bufferSize];

            stream = socket.GetStream();
            stream.BeginRead(readBuffer, 0, bufferSize, OnRecieveData, null);
        }

        void OnRecieveData(IAsyncResult result)
        {
            try
            {
                int bytesLength = stream.EndRead(result);

                if (bytesLength <= 0)
                {
                    Disconnect();

                    return;
                }

                byte[] data = new byte[bytesLength];

                Buffer.BlockCopy(readBuffer, 0, data, 0, bytesLength);

                handle.HandleData(ID, data);

                stream.BeginRead(readBuffer, 0, bufferSize, OnRecieveData, null);
            }
            catch
            {
                Disconnect();
            }
        }

        void Disconnect()
        {
            if (onClientDisconnect != null) { onClientDisconnect(ID); }

            socket.Close();
            socket = null;

            stream = null;
            buffer = null;

            onClientDisconnect = null;
        }
    }
}
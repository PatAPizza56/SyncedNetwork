using System;
using System.Net.Sockets;

namespace SyncedNetwork.Server
{
    public class ServerClient
    {
        public int ID;
        public string IP;
        public bool IsActive;
        public TcpClient socket;
        public NetworkStream stream;
        public ByteBuffer buffer;

        ServerHandle handle;
        ServerTCP tcp;
        Server.OnClientDisconnect onClientDisconnect;

        int bufferSize = 4096;
        byte[] readBuffer;

        public ServerClient(ServerHandle handle, ServerTCP tcp)
        {
            this.handle = handle;
            this.tcp = tcp;
            this.onClientDisconnect = null;

            this.IsActive = false;
        }
        public ServerClient(ServerHandle handle, ServerTCP tcp, Server.OnClientDisconnect onClientDisconnect)
        {
            this.handle = handle;
            this.tcp = tcp;
            this.onClientDisconnect = onClientDisconnect;

            this.IsActive = false;
        }

        public void InitializeClient()
        {
            socket.SendBufferSize = bufferSize;
            socket.ReceiveBufferSize = bufferSize;

            readBuffer = new byte[bufferSize];

            stream = socket.GetStream();
            stream.BeginRead(readBuffer, 0, bufferSize, OnRecieveData, null);

            IsActive = true;
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

            IsActive = false;

            socket.Close();
            socket = null;

            stream = null;
            buffer = null;

            onClientDisconnect = null;

            tcp.UpdateServerClients();
        }
    }
}
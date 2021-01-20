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

        int bufferSize = 4096;
        byte[] readBuffer;

        public ServerHandle handle;

        public ServerClient(ServerHandle handle)
        {
            this.handle = handle;
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
                    CloseSocket();

                    return;
                }

                byte[] data = new byte[bytesLength];

                Buffer.BlockCopy(readBuffer, 0, data, 0, bytesLength);

                //FIX LATER
                handle.HandleData(ID, data);

                stream.BeginRead(readBuffer, 0, bufferSize, OnRecieveData, null);
            }
            catch
            {
                CloseSocket();
            }
        }

        void CloseSocket()
        {
            Console.WriteLine("Connection from {0} has been terminated", IP);

            socket.Close();
            socket = null;
        }
    }
}
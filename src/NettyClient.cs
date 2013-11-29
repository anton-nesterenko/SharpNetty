using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharpNetty
{
    public sealed class NettyClient : Netty
    {
        public delegate void Handle_ConnectionChange();

        public Handle_ConnectionChange Handle_ConnectionLost;

        public NettyClient(bool noDelay = false)
            : base(noDelay)
        {
        }

        public bool Connected
        {
            get
            {
                return this.GetIsConnected(_mainSocket);
            }
        }

        /// <summary>
        /// Establishes a connection with a server at the specified IP & Port.
        /// </summary>
        /// <param name="ip">IP address of the desired server.</param>
        /// <param name="port">Port that the desired server is listening on. </param>
        /// <param name="attemptCount">Number of times to try and connect to the specified server.</param>
        /// <returns>Returns true if a connection was established.</returns>
        public bool Connect(string ip, int port, byte attemptCount)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            for (int i = 0; i < attemptCount; i++)
            {
                try
                {
                    _mainSocket.Connect(endPoint);
                    new Thread(x => BeginReceiving(_mainSocket, 0)).Start();
                    Console.WriteLine("[NettyClient] Connection established with " + ip + ":" + port);
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Disconnects the client from any established connections.
        /// </summary>
        public void Disconnect()
        {
            bool noDelay = _mainSocket.NoDelay;
            _mainSocket.Dispose();
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _mainSocket.NoDelay = noDelay;
        }

        /// <summary>
        /// Sends a packet over the main socket
        /// </summary>
        public void SendPacket(Packet packet)
        {
            this.SendPacket(_mainSocket, packet);
        }
    }
}
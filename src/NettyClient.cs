using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharpNetty
{
    public sealed class NettyClient : Netty
    {
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

            for (int i = 0; i < attemptCount && !_mainSocket.Connected; i++)
            {
                try
                {
                    _mainSocket.Connect(endPoint);
                    new Thread(x => BeginReceiving(_mainSocket, 0)).Start();
                    Console.WriteLine("Connection established with " + ip + ":" + port);
                    return true;
                }
                catch (SocketException) { }
            }

            return false;
        }

        /// <summary>
        /// Disconnects the client from any established connections.
        /// </summary>
        public void Disconnect()
        {
            _mainSocket.Disconnect(false);
            _mainSocket.Dispose();
        }

        /// <summary>
        /// Sends a packet over the main socket
        /// </summary>
        /// <param name="packetBuffer">PackBuffer containing the packet's information</param>
        public void SendPacket(Packet packet, bool forceSend = false)
        {
            SendPacket(packet, _mainSocket, forceSend);
        }
    }
}
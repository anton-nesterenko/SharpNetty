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

        // 2 second connection timeout
        private const int CONNECTION_TIMEOUT = 2000;

        private bool NoDelay { get; set; }

        public NettyClient(bool noDelay = false)
            : base(noDelay)
        {
            this.NoDelay = noDelay;
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
            for (int i = 0; i < attemptCount; i++)
            {
                try
                {

                    _mainSocket = new Socket(_mainSocket.AddressFamily, _mainSocket.SocketType, _mainSocket.ProtocolType);
                    _mainSocket.NoDelay = this.NoDelay;

                    var result = _mainSocket.BeginConnect(ip, port, null, null);

                    if (result.AsyncWaitHandle.WaitOne(CONNECTION_TIMEOUT, true))
                    {
                        if (_mainSocket.Connected)
                        {
                            new Thread(x => BeginReceiving(_mainSocket, 0)).Start();
                            Console.WriteLine("[NettyClient] Connection established with " + ip + ":" + port);
                            _mainSocket.EndConnect(result);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        _mainSocket.Close();
                    }
                }
                catch (SocketException)
                {
                    _mainSocket.Close();
                    continue;
                }
                catch (InvalidOperationException)
                {
                    _mainSocket.Close();
                    continue;
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
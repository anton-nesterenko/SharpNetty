using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SharpNetty
{
    public sealed class NettyServer : Netty
    {
        public class Connection
        {
            private Socket _socket;

            public Socket Socket
            {
                get { return _socket; }
                internal set { _socket = value; }
            }

            public Connection(Socket socket)
            {
                _socket = socket;
            }

            public void SendPacket(Packet packet)
            {
                byte[] data;

                PacketBuffer packetBuffer = new PacketBuffer();
                packetBuffer.WriteString(packet.PacketID);
                packetBuffer.WriteBytes(packet.PacketBuffer.ReadBytes());

                data = packetBuffer.ReadBytes();

                _socket.Send(BitConverter.GetBytes((short)data.Length));
                _socket.Send(data);
            }
        }

        private int _socketPort;
        private IPEndPoint _socketAddress;
        private Connection[] _connections;

        public delegate void HandleConnectionChange(int socketIndex);

        public HandleConnectionChange Handle_NewConnection;
        public HandleConnectionChange Handle_LostConnection;

        public NettyServer(bool noDelay = false)
            : base(noDelay)
        {
        }

        /// <summary>
        /// Returns the socket at the specified index.
        /// </summary>
        /// <param name="index">Index value at which the socket is stored.</param>
        /// <returns></returns>
        public Connection GetConnection(int index)
        {
            if (_connections[index] == null || _connections[index].Socket == null)
            {
                throw new Exception("Invaid Socket Request!");
            }

            return _connections[index];
        }

        public void RemoveConnection(int index)
        {
            if (_connections[index].Socket.Connected)
            {
                _connections[index].Socket.Disconnect(false);
            }

            if (this.Handle_LostConnection != null)
                this.Handle_LostConnection(index);

            _connections[index].Socket = null;
            _connections[index] = null;
        }

        /// <summary>
        /// Sets the Main-Socket's address to the specified IP & Port.
        /// </summary>
        /// <param name="ip">IP Address that the socket will listen on.</param>
        /// <param name="port">Port value that the socket will listen on.</param>
        private void SetAddress(string ip, int port)
        {
            if (_mainSocket.IsBound)
            {
                throw new Exception("The socket is already bound!");
            }

            _socketPort = port;
            _socketAddress = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void StopListening()
        {
            _mainSocket.Shutdown(SocketShutdown.Receive);
        }

        /// <summary>
        /// Begins listening for and accepting socket connections.
        /// </summary>
        /// <param name="backLog">Maximum amount of pending connections</param>
        public void Listen(int backLog = 25, int maximumConnections = 60)
        {
            _connections = new Connection[maximumConnections];

            new Thread(() =>
            {
                Socket incomingSocket;
                int index = -1;

                if (_socketAddress == null) throw new Exception("You must specifiy the socket address before calling the listen method!");
                if (!_mainSocket.IsBound) throw new Exception("You must bind the socket before calling the listen method!");

                _mainSocket.Listen(backLog);

                Console.WriteLine("Server listening on address: " + _mainSocket.LocalEndPoint);

                while (true)
                {
                    incomingSocket = _mainSocket.Accept();

                    for (int i = 0; i < maximumConnections; i++)
                    {
                        if (_connections[i] == null)
                        {
                            _connections[i] = new Connection(incomingSocket);

                            _connections[i].Socket.NoDelay = _mainSocket.NoDelay;

                            index = i;
                            break;
                        }
                    }

                    Console.WriteLine("Received a connection from: " + incomingSocket.RemoteEndPoint);

                    if (Handle_NewConnection != null) Handle_NewConnection.Invoke(index);

                    new Thread(x => BeginReceiving(incomingSocket, index)).Start();
                }
            }).Start();
        }

        /// <summary>
        /// Binds the socket to the specified address
        /// </summary>
        public void BindSocket(string ip, int port)
        {
            SetAddress(ip, port);
            _mainSocket.Bind(_socketAddress);
        }

        /// <summary>
        /// Sends a packet to the designated remote socket connnection.
        /// </summary>
        /// <param name="packet">Packet object containing the packet's unique information</param>
        /// <param name="socketIndex">Socket ID of the desired remote socket that the packet will be sent to.</param>
        /// <param name="forceSend">Force the current Message Buffer to be sent and flushed.</param>
        public void SendPacket(Packet packet, int socketIndex)
        {
            this.GetConnection(socketIndex).SendPacket(packet);
        }
    }
}
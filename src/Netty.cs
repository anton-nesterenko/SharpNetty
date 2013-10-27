using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace SharpNetty
{
    public abstract class Netty
    {
        protected Socket _mainSocket;
        private List<Packet> _registeredPackets;

        public Netty(bool noDelay = false)
        {
            _registeredPackets = new List<Packet>();
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _mainSocket.NoDelay = noDelay;
            RegisterPackets();
        }

        private void RegisterPacket(Packet packet)
        {
            Console.WriteLine("Registering packet: " + packet.ToString());
            _registeredPackets.Add(packet);
        }

        private void RegisterPackets()
        {
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(Packet)))
                {
                    Packet packet = Activator.CreateInstance(type) as Packet;
                    RegisterPacket(packet);
                }
            }

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(Packet)))
                {
                    Packet packet = Activator.CreateInstance(type) as Packet;
                    RegisterPacket(packet);
                }
            }
        }

        /// <summary>
        /// Returns a packet at the specified index.
        /// </summary>
        /// <param name="index">Index value at which the packet is stored.</param>
        /// <returns></returns>
        public Packet GetPacket(int index)
        {
            if (index > _registeredPackets.Count) throw new Exception("Invalid Packet ID!"); return _registeredPackets[index];
        }

        protected void BeginReceiving(Socket socket, int socketIndex)
        {
            int pLength;
            int curRead;
            byte[] data;
            PacketBuffer packetBuffer;
            Packet execPacket;
            string packetID;
            int readOffset;

            while (socket.Connected)
            {
                try
                {
                    pLength = 2;
                    curRead = 0;
                    data = new byte[pLength];
                    packetBuffer = new PacketBuffer();

                    curRead = socket.Receive(data, 0, pLength, SocketFlags.None);

                    while (curRead < pLength)
                        curRead += socket.Receive(data, curRead, pLength - curRead, SocketFlags.None);

                    curRead = 0;
                    pLength = BitConverter.ToInt16(data, 0);
                    data = new byte[pLength];

                    curRead = socket.Receive(data, 0, pLength, SocketFlags.None);

                    while (curRead < pLength)
                        curRead += socket.Receive(data, curRead, pLength - curRead, SocketFlags.None);

                    packetBuffer.FillBuffer(data);

                    packetID = packetBuffer.ReadString();

                    Packet packet =
                        (from p in _registeredPackets
                         where p.PacketID == packetID
                         select p).First();

                    readOffset = System.Text.ASCIIEncoding.ASCII.GetBytes(packetID).Length + 1;

                    execPacket = Activator.CreateInstance(packet.GetType()) as Packet;
                    execPacket.PacketBuffer.FillBuffer(data);
                    execPacket.PacketBuffer.SetOffset(readOffset);
                    execPacket.Execute(this, socketIndex);
                }

                catch (SocketException ex)
                {
                    if (ex is SocketException)
                    {
                        // If this is our client's incoming data listener,
                        // we should just allow the socket to disconnect, and then notify the deriving class
                        // that the socket has disconnected!
                        if (this.GetType() == typeof(NettyClient))
                        {
                            // Create a new reference of this object and cast it to NettyClient.
                            var nettyClient = this as NettyClient;

                            // Invoke the SocketDisconnected method.
                            nettyClient.Handle_ConnectionLost.Invoke();

                            // We've cleaned up everything here; there's no need to notify the end user.
                            return;
                        }

                        Console.WriteLine("We lost connection with: " + socket.RemoteEndPoint);

                        // Create a new reference of this object and cast it to NettyServer.
                        var nettyServer = this as NettyServer;

                        // Invoke the lost connection delegate.
                        if (nettyServer.Handle_LostConnection != null)
                            nettyServer.Handle_LostConnection.Invoke(socketIndex);

                        nettyServer.RemoveConnection(socketIndex);
                    }
                }
            }
        }
    }
}
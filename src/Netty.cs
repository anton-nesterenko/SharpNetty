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
            // Create a new Generic List instance which will store our Registered Packets; assign this new instance to the variable _registeredPackets.
            _registeredPackets = new List<Packet>();

            // Create a new instance of the class Socket; assign this new instance to the variable _mainSocket.
            // We're using a InterNetwork, TCP, Stream Based connection.
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Disable Nagle's Algo. depedending on the value of noDelay (default false).
            _mainSocket.NoDelay = noDelay;

            // Invoke the method which will register our packets.
            RegisterPackets();
        }

        /// <summary>
        /// Registeres a packet for later use.
        /// </summary>
        /// <param name="packet">Packet to be registered.</param>
        private void RegisterPacket(Packet packet)
        {
            // Output the details of the packet that we're registering.
            Console.WriteLine("Registering packet: " + packet.ToString());

            // Add this new packet instance to our registered packets list (_registeredPackets).
            _registeredPackets.Add(packet);
        }

        private void RegisterPackets()
        {
            // Loop through the assembly that is using this library.
            // Find each Class that extends the abstract class Packet, and register the classes that we find.
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                // If the type is a sub class (derives from) Packet.
                if (type.IsSubclassOf(typeof(Packet)))
                {
                    // Create a new instance of Packet based on type.
                    Packet packet = Activator.CreateInstance(type) as Packet;

                    // Register the new packet.
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
            // If the index is greater than the amount of packets that we currently have, throw an error.
            if (index > _registeredPackets.Count) throw new Exception("Invalid Packet ID!");

            // Return the packet at the given index.
            return _registeredPackets[index];
        }

        protected void BeginReceiving(Socket socket, int socketIndex)
        {
            int pLength;
            int curRead;
            byte[] data;
            DataBuffer dataBuffer;
            Packet execPacket;
            string packetID;
            int readOffset;

            // Continue the attempts to receive data so long as the connection is open.
            while (socket.Connected)
            {
                try
                {
                    // Stores our packet header length.
                    pLength = 4;
                    // Stores the current amount of bytes that have been read.
                    curRead = 0;
                    // Stores the bytes that we have read from the socket.
                    data = new byte[pLength];
                    // Stores our DataBuffer instance.
                    dataBuffer = new DataBuffer();

                    // Attempt to read from the socket.
                    curRead = socket.Receive(data, 0, pLength, SocketFlags.None);

                    // Read any remaining bytes.
                    while (curRead < pLength)
                        curRead += socket.Receive(data, curRead, pLength - curRead, SocketFlags.None);

                    // Set the current read to 0.
                    curRead = 0;
                    // Get the packet length (32 bit integer).
                    pLength = BitConverter.ToInt32(data, 0);
                    // Set the data (byte-buffer) to the size of the packet -- determined by pLength.
                    data = new byte[pLength];

                    // Attempt to read from the socket.
                    curRead = socket.Receive(data, 0, pLength, SocketFlags.None);

                    // Read any remaining bytes.
                    while (curRead < pLength)
                        curRead += socket.Receive(data, curRead, pLength - curRead, SocketFlags.None);

                    // Fill our DataBuffer.
                    dataBuffer.FillBuffer(data);

                    // Get the unique packetID.
                    packetID = dataBuffer.ReadString();

                    // Create a new Packet instance by finding the unique packet in our registered packets by using the packetID.
                    Packet packet =
                        (from p in _registeredPackets
                         where p.PacketID == packetID
                         select p).First();

                    // Create a new instance of Packet based on the registered packet that matched our unique packet id.
                    execPacket = Activator.CreateInstance(packet.GetType()) as Packet;
                    // Fill the packet's DataBuffer.
                    execPacket.DataBuffer.FillBuffer(data);
                    // Offset the DataBuffer read offset (this is due to the fact that we've read the Packet-ID string, and we don't want the user to have to deal with this being left over).
                    readOffset = System.Text.ASCIIEncoding.ASCII.GetBytes(packetID).Length + 1;
                    // Set the DataBuffer's offset to the value of readOffset.
                    execPacket.DataBuffer.SetOffset(readOffset);
                    // Execute the packet.
                    execPacket.Execute(this, socketIndex);
                }

                catch (SocketException)
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

                catch (ObjectDisposedException)
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

        protected void SendPacket(Socket socket, Packet packet)
        {
            DataBuffer dataBuffer = new DataBuffer();
            dataBuffer.WriteString(packet.PacketID);
            dataBuffer.WriteBytes(packet.DataBuffer.ReadBytes());

            byte[] data = dataBuffer.ReadBytes();

            byte[] packetHeader = BitConverter.GetBytes(data.Length);

            int sent = socket.Send(packetHeader);

            while (sent < packetHeader.Length)
                sent += socket.Send(packetHeader, sent, packetHeader.Length - sent, SocketFlags.None);

            sent = socket.Send(data);

            while (sent < data.Length)
                sent += socket.Send(data, sent, data.Length - sent, SocketFlags.None);
        }
    }
}
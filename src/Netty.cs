using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace SharpNetty
{
    public abstract class Netty
    {
        protected Socket _mainSocket;
        private const ushort MAX_MESSAGE_LENGTH = 525;
        private short _messageBufferLength;
        private List<Packet> _messageBuffer;
        private List<Packet> _registeredPackets;
        private bool _sendingPacket;

        public Netty()
        {
            _messageBuffer = new List<Packet>();
            _registeredPackets = new List<Packet>();
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _mainSocket.NoDelay = true;
            RegisterPackets();
        }

        private void RegisterPacket(Packet packet)
        {
            Console.WriteLine("Registering packet: " + packet.ToString());
            _registeredPackets.Add(packet);
            packet.SetPacketID(_registeredPackets.Count - 1);
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
            int packetIndex;

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

                    for (int i = 0; i < data.Length; i++)
                    {
                        packetIndex = packetBuffer.ReadShort();
                        int length = packetBuffer.ReadShort();

                        execPacket = Activator.CreateInstance(_registeredPackets[packetIndex].GetType()) as Packet;
                        execPacket.GetPacketBuffer().FillBuffer(data);
                        execPacket.GetPacketBuffer().SetOffset(i + 4);
                        execPacket.Execute(this, socketIndex);
                        i += length + 4;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ObjectDisposedException)
                    {
                        // If this is our client's incoming data listener,
                        // we should just allow the socket to disconnect, and then notify the deriving class
                        // that the socket has disconnected!
                        if (this.GetType() == typeof(NettyClient))
                        {
                            // Create a new reference of this object and cast it to NettyClient.
                            var nettyClient = this as NettyClient;

                            // Invoke the SocketDisconnected method.
                            // nettyClient.SocketDisconnected();

                            // We've cleaned up everything here; there's no need to notify the end user.
                            return;
                        }

                        Console.WriteLine("We lost connection with: " + socket.RemoteEndPoint);

                        // Create a new reference of this object and cast it to NettyServer.
                        var nettyServer = this as NettyServer;

                        // Invoke the lost connection delegate.
                        if (nettyServer.Handle_LostConnection != null)
                            nettyServer.Handle_LostConnection.Invoke(socketIndex);
                    }
                    else
                    {
                        // We're receiving an exception that we shouldn't handle internally...
                        // We should notify the end user.
                        throw ex;
                    }
                }
            }
        }

        private void SortMessageBuffer(List<Packet> messageBuffer)
        {
            messageBuffer.Sort(
                delegate(Packet p1, Packet p2)
                {
                    return p1.GetPriority().CompareTo(p2.GetPriority());
                }
            );
        }

        /// <summary>
        /// Sends a packet over the specified socket
        /// </summary>
        /// <param name="packetBuffer">Packet object containing the packet's unique information</param>
        /// <param name="socket">Socket containing the remote connection information of the socket that the packet will be sent to.</param>
        protected void SendPacket(Packet packet, Socket socket, bool forceSend)
        {
            Packet tmpPacket;
            byte[] data;
            PacketBuffer packetBuffer;

            try
            {
                // If the MessageBuffer is currently in the process of sending...
                // Halt this packet until the task is complete.
                while (_sendingPacket) ;

                // If the MessageBufferLength has reached its maximum capacity, or the packet's priority is that of
                // Priority.High, or if forceSend is set to true, we need to process and send the MessageBuffer.
                if (_messageBufferLength + packet.GetPacketBuffer().ReadBytes().Length > MAX_MESSAGE_LENGTH
                        || packet.GetPriority() == Packet.Priority.High || forceSend)
                {
                    // We are currently processing and sending our MessageBuffer; therefore, we need to
                    // set _sendingPacket to true in order to maintain cross thread stability.
                    _sendingPacket = true;

                    // Add the passed packet into the MessageBuffer.
                    _messageBuffer.Add(packet);

                    // Increase the MessageBuffer's length by the packet's length.
                    _messageBufferLength += (short)packet.GetPacketBuffer().ReadBytes().Length;

                    // Create a new PacketBuffer object.
                    packetBuffer = new PacketBuffer();

                    // Loop through and sort the packets within our MesageBuffer based on: Priority and its Timestamp.
                    // This is based on the Bubble Sort Alg.
                    for (int i = _messageBuffer.Count - 1; i > 0; i++)
                    {
                        if ((int)_messageBuffer[i].GetPriority() > (int)_messageBuffer[i - 1].GetPriority()
                            || ((int)_messageBuffer[i].GetTimeStamp() < (int)_messageBuffer[i - 1].GetTimeStamp()
                                & _messageBuffer[i].GetPriority() == _messageBuffer[i - 1].GetPriority()))
                        {
                            tmpPacket = _messageBuffer[i - 1];
                            _messageBuffer[i - 1] = _messageBuffer[i];
                            _messageBuffer[i] = tmpPacket;

                            // We need to restart the "Bubble" sort.
                            i = 0;

                            // Continue the loop at the beginning.
                            continue;
                        }
                    }

                    SortMessageBuffer(_messageBuffer);

                    packetBuffer = new PacketBuffer();
                    foreach (var mPacket in _messageBuffer)
                    {
                        packetBuffer.WriteShort((short)mPacket.GetPacketID());
                        packetBuffer.WriteShort((short)mPacket.GetPacketBuffer().ReadBytes().Length);
                        packetBuffer.WriteBytes(mPacket.GetPacketBuffer().ReadBytes());
                        mPacket.GetPacketBuffer().Flush();
                    }

                    data = packetBuffer.ReadBytes();
                    socket.Send(BitConverter.GetBytes((short)data.Length));
                    socket.Send(data);
                    _messageBuffer.Clear();
                    _sendingPacket = false;
                }
                else if (packet.GetPriority() == Packet.Priority.None)
                {
                    _messageBuffer.Add(packet);
                }
                else if (packet.GetPriority() == Packet.Priority.Normal)
                {
                    new Thread(() => { Thread.Sleep(1000); SendPacket(packet, socket, true); }).Start();
                }
            }
            catch (SocketException) { }
        }
    }
}
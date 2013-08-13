using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SharpNetty
{
    public abstract class Netty
    {
        protected Socket _mainSocket;
        private const ushort MAX_MESSAGE_LENGTH = 525;
        private short _messageBufferLength = 0;
        private List<Packet> _messageBuffer = new List<Packet>();
        private List<Packet> _packets = new List<Packet>();
        private bool _sendingPacket;

        public Netty()
        {
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _mainSocket.NoDelay = true;
            RegisterPacket(new SyncPacket());
        }

        /// <summary>
        /// Registeres the specified packet for use.
        /// </summary>
        /// <param name="packet"></param>
        public void RegisterPacket(Packet packet)
        {
            _packets.Add(packet);
            packet.SetPacketID(_packets.Count - 1);
        }

        /// <summary>
        /// Returns a packet at the specified index.
        /// </summary>
        /// <param name="index">Index value at which the packet is stored.</param>
        /// <returns></returns>
        public Packet GetPacket(int index)
        {
            if (index > _packets.Count) throw new Exception("Invalid Packet ID!"); return _packets[index];
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

                        execPacket = Activator.CreateInstance(_packets[packetIndex].GetType()) as Packet;
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
                        // If this is our client's incoming data listener, there's no need to do anything here.
                        if (this.GetType() == typeof(NettyClient)) return;

                        Console.WriteLine("We lost connection with: " + socket.RemoteEndPoint);

                        var nettyServer = this as NettyServer;

                        if (nettyServer.Handle_LostConnection != null)
                            nettyServer.Handle_LostConnection.Invoke(socketIndex);
                    }
                    else throw ex;
                }
            }
        }

        /// <summary>
        /// Sends a packet over the specified socket
        /// </summary>
        /// <param name="packetBuffer">Packet object containing the packet's unique information</param>
        /// <param name="socket">Socket containing the remote connection information of the socket that the packet will be sent to.</param>
        protected void SendPacket(Packet packet, Socket socket, bool forceSend)
        {
            try
            {
                while (_sendingPacket) ;

                if (_messageBufferLength + packet.GetPacketBuffer().ReadBytes().Length > MAX_MESSAGE_LENGTH || packet.GetPriority() == Packet.Priority.High || forceSend)
                {
                    _sendingPacket = true;
                    _messageBuffer.Add(packet);
                    _messageBufferLength += (short)packet.GetPacketBuffer().ReadBytes().Length;

                    Packet tmpPacket;
                    byte[] data;
                    PacketBuffer packetBuffer = new PacketBuffer();

                    for (int i = _messageBuffer.Count - 1; i > 0; i++)
                    {
                        if ((int)_messageBuffer[i].GetPriority() > (int)_messageBuffer[i - 1].GetPriority() || ((int)_messageBuffer[i].GetTimeStamp() < (int)_messageBuffer[i - 1].GetTimeStamp() && _messageBuffer[i].GetPriority() == _messageBuffer[i - 1].GetPriority()))
                        {
                            tmpPacket = _messageBuffer[i - 1];
                            _messageBuffer[i - 1] = _messageBuffer[i];
                            _messageBuffer[i] = tmpPacket;
                            i = 0;
                            continue;
                        }
                    }

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
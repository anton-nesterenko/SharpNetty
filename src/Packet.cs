using System;
using System.Net.Sockets;

namespace SharpNetty
{
    public abstract class Packet
    {
        private DataBuffer m_dataBuffer = new DataBuffer();

        public DataBuffer DataBuffer
        {
            get { return m_dataBuffer; }
        }

        public int SocketIndex
        {
            get;
            internal set;
        }

        /// <summary>
        /// Specifies the unique identity for this packet.
        /// </summary>
        public abstract int PacketID
        {
            get;
        }

        public abstract void Execute(Netty netty);
    }
}
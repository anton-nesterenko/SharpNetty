using System;
using System.Net.Sockets;

namespace SharpNetty
{
    public abstract class Packet
    {
        private int _timeStamp;

        private DataBuffer _dataBuffer = new DataBuffer();

        public DataBuffer DataBuffer
        {
            get { return _dataBuffer; }
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

        public Packet()
        {
            _timeStamp = Environment.TickCount;
        }

        public int GetTimeStamp()
        {
            return _timeStamp;
        }

        public abstract void Execute(Netty netty);
    }
}
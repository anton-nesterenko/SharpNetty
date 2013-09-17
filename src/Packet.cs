using System;
using System.Net.Sockets;

namespace SharpNetty
{
    public abstract class Packet
    {
        public enum Priority
        {
            None,
            Normal,
            High
        }

        public Packet()
        {
            this.PacketPriority = Priority.Normal;
            _timeStamp = Environment.TickCount;
        }

        private Priority _priority;

        public Priority PacketPriority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        private int _timeStamp;

        public int TimeStamp
        {
            get { return _timeStamp; }
        }

        public readonly PacketBuffer PacketBuffer = new PacketBuffer();

        public abstract void Execute(Netty netty, int socketIndex);

        public abstract string UniquePacketID { get; }
    }
}
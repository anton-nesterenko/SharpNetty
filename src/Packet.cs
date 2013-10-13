using System;
using System.Net.Sockets;

namespace SharpNetty
{
    public abstract class Packet
    {
        public Packet()
        {
            _timeStamp = Environment.TickCount;
        }

        private int _timeStamp;

        public int GetTimeStamp()
        {
            return _timeStamp;
        }

        private PacketBuffer _packetBuffer = new PacketBuffer();

        public PacketBuffer PacketBuffer
        {
            get { return _packetBuffer; }
        }

        public abstract void Execute(Netty netty, int socketIndex);

        public abstract string PacketID
        {
            get;
        }
    }
}
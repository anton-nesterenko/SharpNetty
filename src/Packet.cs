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
            SetPriority(Priority.Normal);
            _timeStamp = Environment.TickCount;
        }

        private Priority _priority;

        public void SetPriority(Priority priority)
        {
            _priority = priority;
        }

        public Priority GetPriority()
        {
            return _priority;
        }

        private int _timeStamp;

        public int GetTimeStamp()
        {
            return _timeStamp;
        }

        private int _packetID;

        public int GetPacketID()
        {
            return _packetID;
        }

        public void SetPacketID(int value)
        {
            _packetID = value;
        }

        private readonly PacketBuffer _packetBuffer = new PacketBuffer();

        public PacketBuffer GetPacketBuffer()
        {
            return _packetBuffer;
        }

        public abstract void Execute(Netty netty, int socketIndex);
    }
}
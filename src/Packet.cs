namespace SharpNetty
{
    public abstract class Packet
    {
        private readonly DataBuffer _dataBuffer = new DataBuffer();

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

        public abstract void Execute(Netty netty);
    }
}
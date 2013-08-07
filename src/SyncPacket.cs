using System;

namespace SharpNetty
{
    internal class SyncPacket : Packet
    {
        private int _connectionID;

        public SyncPacket()
            : base(Priority.High)
        {
        }

        public void SetConnectionID(int value)
        {
            _connectionID = value;
        }

        public void WriteData()
        {
            GetPacketBuffer().WriteInteger(_connectionID);
            GetPacketBuffer().WriteLong(Environment.TickCount);
        }

        public override void Execute(Netty netty)
        {
            int connectionID = GetPacketBuffer().ReadInteger();
            long value = GetPacketBuffer().ReadLong();

            if (netty.GetType() == typeof(NettyServer))
            {
                long value2 = GetPacketBuffer().ReadLong();

                NettyServer nettyServer = netty as NettyServer;

                // Set the Tick Offset based on the two recorded times + an additional 400 milliseconds as a guessed amount of time the SyncPacket took to process and transmit.
                nettyServer.GetConnection(connectionID).SetTickOffset(Math.Abs(value - value2) + 100);

                Console.WriteLine(nettyServer.GetConnection(connectionID).GetTickOffset());

                return;
            }

            GetPacketBuffer().Flush();
            GetPacketBuffer().WriteInteger(connectionID);
            GetPacketBuffer().WriteLong(value);
            GetPacketBuffer().WriteLong(Environment.TickCount);
            NettyClient nettyClient = netty as NettyClient;
            nettyClient.SendPacket(this);
        }
    }
}
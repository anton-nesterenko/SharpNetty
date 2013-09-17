using System;
using System.Net.Sockets;

namespace SharpNetty
{
    internal class SyncPacket : Packet
    {
        private int _connectionID;

        public void SetConnectionID(int value)
        {
            _connectionID = value;
        }

        public void WriteData()
        {
            this.PacketPriority = Priority.High;
            this.PacketBuffer.WriteInteger(_connectionID);
            this.PacketBuffer.WriteLong(Environment.TickCount);
        }

        public override void Execute(Netty netty, int socketIndex)
        {
            int connectionID = this.PacketBuffer.ReadInteger();
            long value = this.PacketBuffer.ReadLong();

            if (netty.GetType() == typeof(NettyServer))
            {
                long value2 = this.PacketBuffer.ReadLong();

                NettyServer nettyServer = netty as NettyServer;

                // Set the server tick offset based on the two ticks + a 100ms latency (guess).
                // This will be improved in the future.
                nettyServer.GetConnection(connectionID).SetTickOffset(Math.Abs(value - value2) + 100);

                Console.WriteLine(nettyServer.GetConnection(connectionID).GetTickOffset());

                return;
            }

            this.PacketBuffer.Flush();
            this.PacketBuffer.WriteInteger(connectionID);
            this.PacketBuffer.WriteLong(value);
            this.PacketBuffer.WriteLong(Environment.TickCount);
            NettyClient nettyClient = netty as NettyClient;
            nettyClient.SendPacket(this);
        }

        public override string UniquePacketID
        {
            get
            {
                return "SyncPacket";
            }
        }
    }
}
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
            SetPriority(Priority.High);
            GetPacketBuffer().WriteInteger(_connectionID);
            GetPacketBuffer().WriteLong(Environment.TickCount);
        }

        public override void Execute(Netty netty, int socketIndex)
        {
            int connectionID = GetPacketBuffer().ReadInteger();
            long value = GetPacketBuffer().ReadLong();

            if (netty.GetType() == typeof(NettyServer))
            {
                long value2 = GetPacketBuffer().ReadLong();

                NettyServer nettyServer = netty as NettyServer;

                // Set the server tick offset based on the two ticks + a 100ms latency (guess).
                // This will be improved in the future.
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

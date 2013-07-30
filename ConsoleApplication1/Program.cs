using SharpNetty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            NettyServer netServer = new NettyServer();
            netServer.BindSocket("127.0.0.1", 4000);
            netServer.Listen();

            NettyClient netClient = new NettyClient();
            netClient.Connect("127.0.0.1", 4000, 1);
        }
    }
}
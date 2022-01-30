using System;
using System.Net;
using System.Net.Sockets;

namespace StupidNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new DualityServer(11000, 100);
            server.Init();
            server.Start();
        }
    }
}

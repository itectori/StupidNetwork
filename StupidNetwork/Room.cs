using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace StupidNetwork
{
    class Room
    {
        public Room(SocketAsyncEventArgs p1, SocketAsyncEventArgs p2)
        {
            player1 = p1;
            player2 = p2;
        }

        public SocketAsyncEventArgs player1 { get; }
        public SocketAsyncEventArgs player2 { get; }
    }
}

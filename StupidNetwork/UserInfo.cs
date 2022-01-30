using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;


namespace StupidNetwork
{
    enum UserState
    {
        NotConnected,
        Waiting,
        Playing,
    }

    class UserInfo
    {
        public int BufferIndex { get; }

        public Socket Socket;
        public string Id;
        public UserState State;
        public Room Room;

        public UserInfo(int bufferIndex)
        {
            BufferIndex = bufferIndex;
        }
    }
}

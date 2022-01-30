using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace StupidNetwork
{

    enum SendType
    {
        SendId,
        JoinLobyOk,
        SyncValue,
    }

    enum ReceiveType
    {
        JoinLoby,
        SyncValue,
    }

    class DualityServer : Server
    {
        readonly Dictionary<string, SocketAsyncEventArgs> waitingLoby = new();

        static readonly Random rand = new();

        public DualityServer(int port, int nbMaxConnections) : base(port, nbMaxConnections)
        {
            OnAccepted += DualityServer_OnAccepted;
            OnClosed += DualityServer_OnClosed;
            OnReceived += DualityServer_OnReceived;
        }

        private string GenerateUniqueID()
        {
            string id;
            do
            {
                id = rand.Next(1000, 9999).ToString();
            } while (waitingLoby.ContainsKey(id));
            return id;
        }

        private void DualityServer_OnAccepted(object sender, SocketAsyncEventArgs e)
        {
            var token = e.UserToken as UserInfo;
            var id = GenerateUniqueID();
            token.Id = id;
            token.State = UserState.Waiting;
            waitingLoby.Add(id, e);
            Send(SendType.SendId, id, e);
        }

        private void DualityServer_OnClosed(object sender, SocketAsyncEventArgs e)
        {
            var token = e.UserToken as UserInfo;
            var oldState = token.State;
            token.State = UserState.NotConnected;

            switch (oldState)
            {
                case UserState.NotConnected:
                    break;

                case UserState.Waiting:
                    waitingLoby.Remove(token.Id);
                    break;
                case UserState.Playing:
                    TryDisconnect(token.Room.player1);
                    TryDisconnect(token.Room.player2);
                    break;
            }

        }

        private void TryDisconnect(SocketAsyncEventArgs e)
        {
            var socket = (e.UserToken as UserInfo)?.Socket;
            if (socket == null || !socket.Connected)
            {
                return;
            }
            CloseClientSocket(e);
        }

        private void DualityServer_OnReceived(object sender, SocketAsyncEventArgs e)
        {
            UserInfo token = (UserInfo)e.UserToken;
            var dataString = Encoding.ASCII.GetString(buffers[token.BufferIndex], e.Offset, e.BytesTransferred);

            var parts = dataString.Split("|");
            for (int i = 0; i < parts.Length; i++)
            {
                ProcessData(parts[i], e);
            }

        }

        private void ProcessData(string dataString, SocketAsyncEventArgs e)
        {
            if (dataString.Length == 0)
            {
                return;
            }
            var dataSplit = dataString.Split();
            if (!int.TryParse(dataSplit[0], out int receiveType))
            {
                Console.WriteLine("Invalid format");
                return;
            }
            switch ((ReceiveType)receiveType)
            {
                case ReceiveType.JoinLoby:
                    JoinLoby(e, dataSplit[1]);
                    break;
                case ReceiveType.SyncValue:
                    SyncValue(e, dataString);
                    break;
                default:
                    Console.WriteLine("Invalid Receive Type");
                    return;
            }
        }

        private void JoinLoby(SocketAsyncEventArgs e, string loby)
        {
            if (!waitingLoby.ContainsKey(loby) || waitingLoby[loby] == e)
            {
                return;
            }

            var p1 = waitingLoby[loby];
            var p2 = e;
            var token1 = p1.UserToken as UserInfo;
            var token2 = p2.UserToken as UserInfo;
            waitingLoby.Remove(loby);
            waitingLoby.Remove(token2.Id);
            token1.State = UserState.Playing;
            token2.State = UserState.Playing;

            Room room = new(p1, p2);
            token1.Room = room;
            token2.Room = room;

            Send(SendType.JoinLobyOk, loby, p1);
            Send(SendType.JoinLobyOk, loby, p2);

            Console.WriteLine($"New room created : {token1.Id} { token2.Id}");
        }

        private void SyncValue(SocketAsyncEventArgs e, string data)
        {
            var token = e.UserToken as UserInfo;
            SocketAsyncEventArgs dest;
            if (token.Room.player1 == e)
            {
                dest = token.Room.player2;
            }
            else
            {
                dest = token.Room.player1;
            }
            Send(SendType.SyncValue, data, dest);

        }

        private void Send(SendType actionType, string data, SocketAsyncEventArgs e)
        {
            Send($"{(int)actionType} {data}|", e);
        }
    }
}

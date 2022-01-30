using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StupidNetwork
{

    class Server
    {
        private int nbMaxConnections;
        private int port;
        Socket listener;
        protected byte[][] buffers { get; }

        Stack<SocketAsyncEventArgs> socketEventArgsPool = new();

        public event EventHandler<SocketAsyncEventArgs> OnAccepted;
        public event EventHandler<SocketAsyncEventArgs> OnReceived;
        public event EventHandler<SocketAsyncEventArgs> OnSent;
        public event EventHandler<SocketAsyncEventArgs> OnClosed;

        public Server(int port, int nbMaxConnections)
        {
            this.port = port;
            this.nbMaxConnections = nbMaxConnections;
            buffers = new byte[nbMaxConnections][];
        }

        public void Init()
        {
            for (int i = 0; i < nbMaxConnections; i++)
            {
                buffers[i] = new byte[1024];

                // init socket event args pool
                var readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += ProcessReceive;
                readWriteEventArg.UserToken = new UserInfo(i);
                readWriteEventArg.SetBuffer(buffers[i]);
                socketEventArgsPool.Push(readWriteEventArg);
            }
        }

        public void Start()
        {
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            this.listener.Listen(100);
            Console.WriteLine($"Socket listening on : {localEndPoint.Address}:{port}");

            var acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += AcceptEventArg_Completed;
            StartAccept(acceptEventArg);

            ConsoleKey key;
            do
            {
                Console.WriteLine("Press escape to terminate the server process....");
                key = Console.ReadKey().Key;
            } while (key != ConsoleKey.Escape);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            if (!listener.AcceptAsync(acceptEventArg))
            {
                ProcessAccept(acceptEventArg);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgs socketEventArg = socketEventArgsPool.Pop();
            ((UserInfo)socketEventArg.UserToken).Socket = e.AcceptSocket;
            Console.WriteLine("Client connection accepted.");
            OnAccepted?.Invoke(this, socketEventArg);

            if (!e.AcceptSocket.ReceiveAsync(socketEventArg))
            {
                ProcessReceive(this, socketEventArg);
            }

            StartAccept(e);
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                UserInfo token = (UserInfo)e.UserToken;
                var dataString = Encoding.ASCII.GetString(buffers[token.BufferIndex], e.Offset, e.BytesTransferred);
                Console.WriteLine($"Received : {dataString}");
                OnReceived?.Invoke(this, e);

                if (!token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(this, e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        protected void Send(string data, SocketAsyncEventArgs e)
        {
            var token = e.UserToken as UserInfo;
            var buffer = Encoding.ASCII.GetBytes(data);
            token.Socket.Send(buffer);
            Console.WriteLine($"Sent : {data}");
            OnSent?.Invoke(this, e);
        }

        protected void CloseClientSocket(SocketAsyncEventArgs e)
        {
            UserInfo token = e.UserToken as UserInfo;
            if (token.Socket == null)
            {
                return;
            }

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            finally
            {
                token.Socket.Close();
            }
            token.Socket = null;

            socketEventArgsPool.Push(e);
            Console.WriteLine("A client has been disconnected from the server.");
            OnClosed?.Invoke(this, e);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace AsyncStream
{
    class SocketStreamServer : SocketStreamBase
    {
        public SocketStreamServer()
        {
            StartListening();
        }

        protected int Port = 15155;
        protected List<SocketStreamConnection> _Connections;

        private void StartListening()
        {
            /* http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.listen(v=vs.110).aspx */

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress hostIP = (Dns.GetHostEntry(IPAddress.Any.ToString())).AddressList[0];
            listenSocket.Bind(new IPEndPoint(hostIP, Port));

            listenSocket.BeginAccept(ClientConnected, listenSocket);  
        }

        private void ClientConnected(IAsyncResult result)
        {
            Socket listenSocket = result.AsyncState as Socket;
            Socket connected = listenSocket.EndAccept(result);
            if (connected.Connected)
            {
                SocketStreamConnection Connection = new SocketStreamConnection(connected);
                Connection.MessageReceived += new MessageEventHandler(Connection_MessageReceived);
                _Connections.Add(Connection);
            }

            listenSocket.BeginAccept(ClientConnected, listenSocket); 
        }

        private void Connection_MessageReceived(object sender, MessageEventArgs args)
        {
            this.OnMessageReceived(args);
        }
    }
}

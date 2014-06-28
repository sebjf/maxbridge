using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using AsyncStream;

namespace Messaging
{
    public class SocketStreamServer
    {
        public delegate void NewConnectionRequestDelegate(SocketStreamConnection client_connection);
        
        protected int m_port = 15155;
        protected NewConnectionRequestDelegate m_connectionHandler;

        private Socket listenSocket;

        public SocketStreamServer(int port, NewConnectionRequestDelegate clientConnectionHandler)
        {
            m_port = port;
            m_connectionHandler = clientConnectionHandler;
            StartListening();
        }

        private void StartListening()
        {
            /* http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.listen(v=vs.110).aspx */

            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress hostIP = Dns.GetHostAddresses("localhost").Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray()[0];
            listenSocket.Bind(new IPEndPoint(hostIP, m_port));
            listenSocket.Listen(10);

            listenSocket.BeginAccept(ClientConnected, listenSocket);  
        }

        ~SocketStreamServer()
        {
            if (listenSocket != null)
            {
                listenSocket.Close();
            }
        }

        private void ClientConnected(IAsyncResult result)
        {
            Socket listenSocket = result.AsyncState as Socket;
            Socket connected = listenSocket.EndAccept(result);
            if (connected.Connected)
            {
                m_connectionHandler(new SocketStreamConnection(connected));
            }

            listenSocket.BeginAccept(ClientConnected, listenSocket); 
        }
    }
}

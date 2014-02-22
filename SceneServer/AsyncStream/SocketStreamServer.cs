using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace AsyncStream
{
    public class SocketStreamServer : SocketStreamBase
    {
        public SocketStreamServer()
        {
            StartListening();
        }

        protected int Port = 15155;
        protected List<SocketStreamConnection> _Connections = new List<SocketStreamConnection>();

        private void StartListening()
        {
            /* http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.listen(v=vs.110).aspx */

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress hostIP = Dns.GetHostAddresses("localhost").Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray()[0];
            listenSocket.Bind(new IPEndPoint(hostIP, Port));
            listenSocket.Listen(10);

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

        public override void Disconnect()
        {
            lock (this._Connections)
            {
                foreach (SocketStreamConnection connection in this._Connections)
                {
                    try
                    {
                        connection.Disconnect();
                    }
                    catch
                    {
                    }
                    this._Connections.Clear();
                }
            }
        }

        public override void SendMessage(byte[] message)
        {
            List<SocketStreamConnection> list = null;
            bool flag = false;
            lock (this._Connections)
            {
                foreach (SocketStreamConnection connection in this._Connections)
                {
                    try
                    {
                        flag = !connection.IsConnected;
                        if (!flag)
                        {
                            connection.SendMessage(message);
                        }
                    }
                    catch
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        try
                        {
                            connection.Disconnect();
                        }
                        catch
                        {
                        }
                        if (list == null)
                        {
                            list = new List<SocketStreamConnection>();
                        }
                        list.Add(connection);
                    }
                }
                if (list != null)
                {
                    foreach (SocketStreamConnection connection in list)
                    {
                        this._Connections.Remove(connection);
                    }
                }
            }
        }
    }
}

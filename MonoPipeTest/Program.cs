using System;
using System.IO.Pipes;
using AsyncStream;
using System.Threading;

namespace MonoPipeTest
{
    class Program
    {

        static ManualResetEvent resetEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            server = new SocketStreamServer(15155);
            server.MessageReceived += new MessageEventHandler(server_MessageReceived);

            while (true) { }
        }

        static SocketStreamServer server;

        static void server_MessageReceived(object sender, MessageEventArgs args)
        {
            server.SendMessage(new byte[] { 1, 2, 3 });
        }
    }
}

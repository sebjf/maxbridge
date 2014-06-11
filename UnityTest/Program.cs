using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Net;
using Messaging;

namespace UnityTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Test3();
        }

        static void Test3()
        {
            IPAddress[] IPs = Dns.GetHostAddresses("localhost");

            Socket s = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            s.Connect(IPs[1], 15155);
            Console.WriteLine("Connection established");

            do
            {
                s.Send(new byte[] { 5, 6, 7, 8 }, SocketFlags.None);

                System.Console.WriteLine("Sent: 5 6 7 8");

                byte[] data = new byte[4];
                s.Receive(data, 4, SocketFlags.None);

                System.Console.WriteLine(string.Format("Received: {0} {1} {2} {3}", data[0], data[1], data[2], data[3]));

            } while (true);
        }

        static void Test2()
        {
            NamedPipeServerStream stream = new NamedPipeServerStream("MaxUnityBridge");
            stream.WaitForConnection();

            do
            {
                byte[] data = new byte[4];
                stream.Read(data, 0, 4);

                System.Console.WriteLine(string.Format("Received: {0} {1} {2} {3}", data[0], data[1], data[2], data[3]));

                stream.Write(new byte[] { 5, 6, 7, 8 }, 0, 4);

                System.Console.WriteLine("Sent: 5 6 7 8");

                stream.Flush();
                stream.WaitForPipeDrain();


            } while (true);
        }

    }
}

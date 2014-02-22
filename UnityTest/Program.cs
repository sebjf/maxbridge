using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using AsyncPipes;
using Messages;

namespace UnityTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Test2();
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

        static void Test1()
        {
            NamedPipeServerStream stream = new NamedPipeServerStream("MaxUnityBridge");
            stream.WaitForConnection();

            do
            {
                byte[] dataLength = new byte[4];
                stream.Read(dataLength, 0, 4);
                int messageLength = BitConverter.ToInt32(dataLength, 0);
                byte[] data = new byte[messageLength];
                stream.Read(data, 0, messageLength);

                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(data);

                switch (message.MessageType)
                {
                    case MessageTypes.RequestGeometry:

                        UnityMessage reply = new MessageGeometryUpdateStream();
                        byte[] replyData = MessageSerializers.SerializeMessage(reply);
                        stream.Write(BitConverter.GetBytes(replyData.Length), 0, 4);
                        stream.Write(replyData, 0, replyData.Length);
                        stream.Flush();
                        stream.WaitForPipeDrain();

                        break;
                    default:

                        break;
                }

            } while (true);
        }
    }
}

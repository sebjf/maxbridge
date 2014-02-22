using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using System.IO.Pipes;
using System.IO;
using AsyncPipes;
using System.ComponentModel;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using Messages;
using Winterdom.IO.FileMap;

namespace MaxExporter
{
    /* This is mostly to ensure that the messages are prepended with the length by hiding the original send methods */
    public class NamedPipeSimpleServer
    {
        NamedPipeStreamServer pipe;

        public NamedPipeSimpleServer(string Name, MessageReceiveHandler ReceiveEventHandler)
        {
            try
            {
                pipe = new NamedPipeStreamServer(Name);
                pipe.MessageReceived += new MessageEventHandler(ReceiveMessage);
                messageReceiveHandler = ReceiveEventHandler; 
            }
            catch
            {
                Log.Add("Could not start MaxUnityBridge");
            }
        }

        public void SendMessage(UnityMessage message)
        {
            byte[] messageData = MessageSerializers.SerializeObject(message);
            byte[] messageLen = BitConverter.GetBytes(messageData.Length);
            pipe.SendMessage(messageLen.Concat(messageData).ToArray());
        }

        protected void ReceiveMessage(object sender, MessageEventArgs args)
        {
            Log.Add("Received message from Unity.");

            try
            {
                int messageLength = BitConverter.ToInt32(args.Message, 0);
                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(args.Message, sizeof(Int32)); //fails because we recieve back the same message just sent!!!???
                if (messageReceiveHandler != null)
                {
                    messageReceiveHandler(message);
                }
            }
            catch (Exception e)
            {
                Log.Add("Recieved message from MaxUnityBridge but could not process it: " + e.Message + e.StackTrace);
            }
        }

        public delegate void MessageReceiveHandler(UnityMessage message);
        protected MessageReceiveHandler messageReceiveHandler;
    }

    public partial class MaxUnityExporter
    {
        protected IGlobal globalInterface;

        public MaxUnityExporter(IGlobal global)
        {
            this.globalInterface = global;
        }

        NamedPipeSimpleServer pipe;
        MemoryMappedFile sharedmemory;
        MapViewStream sharedmemoryview;

        public void StartServer()
        {
            pipe = new NamedPipeSimpleServer("MaxUnityBridge", processMessage);
        }

        public void StopServer()
        {
            //pipe.Disconnect();
        }

        void processMessage(UnityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageTypes.Ping:
                    sendPing();
                    break;

                case MessageTypes.RequestGeometry:
                    sendGeometryMemory();
                    break;

                default:
                    string error = "Recieved unsupported message type: " + message.MessageType.ToString();
                    Log.Add(error);
                    sendError(error);
                    break;
            }
        }

        void sendError(string msg)
        {
            pipe.SendMessage(new MessageError(msg));
        }

        void sendPing()
        {
            pipe.SendMessage(new MessagePing("Hello from Max!"));
        }

        void sendGeometryMemory()
        {
            openSharedMemory(1000000);
            StreamWriter writer = new StreamWriter(sharedmemoryview);
            writer.WriteLine("Hello From Max Via Memory!");
            pipe.SendMessage(new MessageGeometryUpdateMemory(sharedmemory, 0, 0));
        }


        void openSharedMemory(int size)
        {
            if (sharedmemory != null)
            {
                if (sharedmemory.Size < size)
                {
                    if (sharedmemoryview != null)
                    {
                        sharedmemoryview.Close();
                    }
                    if (sharedmemory.IsOpen)
                    {
                        sharedmemory.Close();
                    }
                    sharedmemory = null;
                }
            }

            if (sharedmemory == null)
            {
                string name = Guid.NewGuid().ToString();
                sharedmemory = MemoryMappedFile.Create(MapProtection.PageReadWrite, size, name);
                sharedmemoryview = sharedmemory.MapView(MapAccess.FileMapWrite, 0, size);
            }
        }
    }
}

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
    public partial class MaxUnityExporter
    {
        protected IGlobal globalInterface;

        public MaxUnityExporter(IGlobal global)
        {
            this.globalInterface = global;
        }

        NamedPipeStreamServer pipe;
        MemoryMappedFile sharedmemory;
        MapViewStream sharedmemoryview;

        public void StartServer()
        {
            try
            {
                pipe = new NamedPipeStreamServer("MaxUnityBridge");
                pipe.MessageReceived += new MessageEventHandler(pipe_MessageReceived);
            }
            catch
            {
                Log.Add("Could not start MaxUnityBridge");
            }
        }

        public void StopServer()
        {
            //pipe.Disconnect();
        }

        void  pipe_MessageReceived(object sender, MessageEventArgs args)
        {
            Log.Add("Received message from Unity.");

            try
            {
                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(args.Message); //fails because we recieve back the same message just sent!!!???
                processMessage(message);
            }
            catch(Exception e)
            {
                Log.Add("Recieved message from MaxUnityBridge but could not process it: " + e.Message + e.StackTrace);
            }
        }

        void pipe_MessageSend(MessageTypes type, UnityMessageParams parms)
        {
            var msg = new UnityMessage(type);
            msg.Content = parms;
            pipe.SendMessage(MessageSerializers.SerializeObject(msg));
        }

        void processMessage(UnityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageTypes.Ping:
                    sendPing();
                    break;

                case MessageTypes.RequestGeometry:
                    sendGeometry();
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
            pipe_MessageSend(MessageTypes.Error, new MessageErrorParams(msg));
        }

        void sendPing()
        {
            pipe_MessageSend(MessageTypes.Ping, new MessagePingParams("Hello from Max!"));
        }

        void sendGeometry()
        {
            openSharedMemory(10000);

            byte[] data = Encoding.Default.GetBytes("Geometry!".ToCharArray());
            sharedmemoryview.Write(data);

            pipe_MessageSend(MessageTypes.GeometryUpdate, new MessageGeometryUpdateParams(sharedmemory, 0, data.Length));
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

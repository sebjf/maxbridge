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

        SimpleStreamServer pipe;
        MemoryMappedFile sharedmemory;
        MapViewStream sharedmemoryview;

        public void StartServer()
        {
            pipe = new SimpleStreamServer("MaxUnityBridge", processMessage);
        }

        public void StopServer()
        {
        }

        void processMessage(UnityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageTypes.Ping:
                    sendPing();
                    break;

                case MessageTypes.RequestGeometry:
                    sendGeometryUpdate();
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

        void sendGeometryUpdate()
        {
            sendGeometryStream();
        }

        void sendGeometryMemory()
        {
            openSharedMemory(1000000);
            StreamWriter writer = new StreamWriter(sharedmemoryview);
            writer.WriteLine("Hello From Max Via Memory!");
            pipe.SendMessage(new MessageGeometryUpdateMemory(sharedmemory, 0, 1000000));
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

        void sendGeometryStream()
        {
            pipe.SendMessage(new MessageGeometryUpdateStream(createGeometryUpdate()));
        }
    }
}

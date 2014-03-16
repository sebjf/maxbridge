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
        protected IGlobal gi;

        public MaxUnityExporter(IGlobal global)
        {
            this.gi = global;
        }

        SimpleStreamServer pipe;
        MemoryMappedFile sharedmemory;
        MapViewStream sharedmemoryview;

        public void StartServer()
        {
            pipe = new SimpleStreamServer("MaxUnityBridge", ProcessMessage);
        }

        public void StopServer()
        {
        }

        protected void ProcessMessage(UnityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageTypes.Ping:
                    SendPing();
                    break;

                case MessageTypes.RequestGeometry:
                    SendGeometryUpdate();
                    break;

                default:
                    string error = "Recieved unsupported message type: " + message.MessageType.ToString();
                    Log.Add(error);
                    SendError(error);
                    break;
            }
        }

        protected void SendError(string msg)
        {
            pipe.SendMessage(new MessageError(msg));
        }

        protected void SendPing()
        {
            pipe.SendMessage(new MessagePing("Hello from Max!"));
        }

        protected void SendGeometryUpdate()
        {
            SendGeometryStream();
        }

        protected void SendGeometryMemory()
        {
            OpenSharedMemory(1000000);
            StreamWriter writer = new StreamWriter(sharedmemoryview);
            writer.WriteLine("Hello From Max Via Memory!");
            pipe.SendMessage(new MessageGeometryUpdateMemory(sharedmemory, 0, 1000000));
        }


        protected void OpenSharedMemory(int size)
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

        protected void SendGeometryStream()
        {
            pipe.SendMessage(new MessageGeometryUpdateStream(CreateUpdates()));
        }
    }
}

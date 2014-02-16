using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using AsyncPipes;
using Messages;
using UnityEditor;
using UnityEngine;
using Winterdom.IO.FileMap;


namespace MaxUnityBridge
{
    public class NamedPipeClientStreamSimple
    {
        protected string name;
        protected NamedPipeClientStream pipe;

        public NamedPipeClientStreamSimple(string name)
        {
            this.name = name;
            pipe = new NamedPipeClientStream(name);
        }

        public bool MakeConnection()
        {
            if (pipe == null)
            {
                pipe = new NamedPipeClientStream(name);
            }

            if (!pipe.IsConnected)
            {
                try
                {
                    pipe.Connect(1000);
                }
                catch
                {
                    Debug.Log("Could not find Max!");
                    return false;
                }
            }

            return true;
        }



        public void SendMessage(object message)
        {
            if (MakeConnection())
            {
                byte[] data = MessageSerializers.SerializeObject(message);
                pipe.Write(data, 0, data.Length);
      /*          BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(pipe, message);
                pipe.Flush();
                pipe.WaitForPipeDrain(); */
            }
        }

        public object ReceiveMessage()
        {
            try
            {
                MemoryStream memstream = new MemoryStream();
                byte[] data = new byte[10000];
                while(pipe.
                int read = 0;
                do
                {
                    read = pipe.Read(data, 0, 10000);
                    memstream.Write(data, 0, read);
                } while (read > 0);

                return MessageSerializers.DeserializeObject(memstream.ToArray());
      //          BinaryFormatter formatter = new BinaryFormatter();
       //         return formatter.Deserialize(pipe);
            }
            catch
            {
                Debug.Log("Could not recieve message, there may be garbage in the pipe. Closing it...");
                try
                {
                    pipe.Dispose();
                }
                catch { }
                pipe = null;
                return null;
            }
        }

        public T RecieveMessage<T>()
        {
            return (T)ReceiveMessage();
        }
    }

    public class UnityImporter
    {
        public UnityImporter()
        {
            pipe = new NamedPipeClientStreamSimple("MaxUnityBridge");
        }

        protected NamedPipeClientStreamSimple pipe;
        protected MemoryMappedFile sharedmemory;
        protected MapViewStream sharedmemoryview;

        protected BinaryFormatter formatter = new BinaryFormatter();

        public void DoImport()
        {
            Debug.Log("Beginning import");

            try
            {
                UnityMessage msg = new UnityMessage(MessageTypes.RequestGeometry);
                pipe.SendMessage(msg);
            }
            catch(Exception e)
            {
                Debug.Log("Could not send request: " + e.Message + e.StackTrace);
            }

            object m = pipe.ReceiveMessage();
            processMessage(m as UnityMessage);

            Debug.Log("Update Complete.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">Deserialised message to process</param>
        /// <returns>Whether another message is expected, or whether this update is complete.</returns>
        protected void processMessage(UnityMessage message)
        {
            if (message == null)
            {
                Debug.Log("Did not recieve valid message from Max!");
            }

            switch (message.MessageType)
            {
                case MessageTypes.Ping:
                    Debug.Log((message.Content as MessagePingParams).message);
                    break;

                case MessageTypes.GeometryUpdate:
                    updateGeometry(message.Content as MessageGeometryUpdateParams);
                    break;

                case MessageTypes.Error:
                    Debug.Log("Max encountered an error: " + (message.Content as MessageErrorParams).message);
                    break;
            }
        }

        protected void updateGeometry(MessageGeometryUpdateParams message)
        {
            openSharedMemory(message.sharedMemory);

            byte[] data = new byte[message.length];
            sharedmemoryview.Read(data, (int)message.geometryOffset, (int)message.length);
            string s = Encoding.Default.GetString(data);
            Debug.Log(s);
        }

        protected void openSharedMemory(SharedMemoryInfo sharedMemoryInfo)
        {
            if (sharedmemoryview != null)
            {
                sharedmemoryview.Close();
            }

            if (sharedmemory != null)
            {
                sharedmemory.Close();
            }

            sharedmemory = MemoryMappedFile.Open(MapAccess.FileMapRead, sharedMemoryInfo.name);
            sharedmemoryview = sharedmemory.MapView(MapAccess.FileMapRead, 0, sharedMemoryInfo.size);
        }
    }
}

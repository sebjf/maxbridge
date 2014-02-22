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
    public class NamedPipeSimpleClient
    {
        protected string name;
        protected NamedPipeClientStream pipe;

        public NamedPipeSimpleClient(string name)
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
                byte[] dataLength = BitConverter.GetBytes(data.Length);

                pipe.Write(dataLength, 0, dataLength.Length);
                pipe.Flush();
                pipe.WaitForPipeDrain();

                pipe.Write(data, 0, data.Length);
                pipe.Flush();
                pipe.WaitForPipeDrain();
            }
        }

        public object ReceiveMessage()
        {
            try
            {
                byte[] dataLength = new byte[4];
                pipe.Read(dataLength, 0, 4);
                int msglength = BitConverter.ToInt32(dataLength, 0);

                byte[] data = new byte[msglength];
                
                int read = 0;
                do
                {
                    read += pipe.Read(data, read, msglength - read);
                } while (read < msglength);

                return MessageSerializers.DeserializeObject(data);
      //        BinaryFormatter formatter = new BinaryFormatter();
      //        return formatter.Deserialize(pipe);
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
            pipe = new NamedPipeSimpleClient("MaxUnityBridge");
        }

        protected NamedPipeSimpleClient pipe;
        protected MemoryMappedFile sharedmemory;
        protected MapViewStream sharedmemoryview;

        protected BinaryFormatter formatter = new BinaryFormatter();

        public void Test()
        {
            NamedPipeClientStream pipe = new NamedPipeClientStream("MaxUnityBridge");
            pipe.Connect();

            do
            {

                UnityMessage message = new UnityMessage(MessageTypes.RequestGeometry);

          //      byte[] data = MessageSerializers.SerializeObject(message);
          //      byte[] dataLength = BitConverter.GetBytes(data.Length);

                byte[] data1 = new byte[] { 1, 2, 3, 4 };

                pipe.Write(data1, 0, data1.Length);
                pipe.Flush();
                pipe.WaitForPipeDrain();

                byte[] datarx1 = new byte[4];
                pipe.Read(datarx1, 0, 4);

            } while (true);
        }

        public void DoImport()
        {
            Test();

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
                    Debug.Log((message as MessagePing).message);
                    break;

                case MessageTypes.GeometryUpdateMemory:
                    updateGeometryMemory(message as MessageGeometryUpdateMemory);
                    break;

                case MessageTypes.GeometryUpdateStream:
                    updateGeometryStream(message as MessageGeometryUpdateStream);
                    break;

                case MessageTypes.Error:
                    Debug.Log("Max encountered an error: " + (message as MessageError).message);
                    break;
            }
        }

        protected void updateGeometryMemory(MessageGeometryUpdateMemory message)
        {
            openSharedMemory(message.sharedMemory);

            StreamReader reader = new StreamReader(sharedmemoryview);
            Debug.Log(reader.ReadLine());
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

        protected void updateGeometryStream(MessageGeometryUpdateStream message)
        {
            DoImport();
        }
    }
}

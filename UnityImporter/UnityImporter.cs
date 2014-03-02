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
    public partial class UnityImporter
    {
        public UnityImporter()
        {
            pipe = new SimpleStreamClient(15155);

            UpdateProcessor = new UpdateProcessor();
        }

        public UpdateProcessor UpdateProcessor { get; set; }

        protected SimpleStreamClient pipe;
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
            UpdateProcessor.ProcessUpdate(message.Geometry);
        }
    }
}

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Messaging;
using UnityEngine;
using MaxUnityBridge.Geometry;


namespace MaxUnityBridge
{
    public partial class UnityImporter
    {

        public UpdateProcessor UpdateProcessor { get; set; }
        protected SimpleStreamClient pipe;
        protected BinaryFormatter formatter = new BinaryFormatter();

        private GeometryProcessor GeometryProcessor = new GeometryProcessor();

        public UnityImporter()
        {
            pipe = new SimpleStreamClient(15155, Debug.Log);
            UpdateProcessor = new UpdateProcessor();
        }

        protected void ProcessIsochronous(UnityMessage message)
        {
            Debug.Log("Beginning import");

            try
            {
                pipe.SendMessage(message);
            }
            catch (Exception e)
            {
                Debug.Log("Could not send request: " + e.Message + e.StackTrace);
            }

            object m = pipe.ReceiveMessage();
            ProcessMessage(m as UnityMessage);

            Debug.Log("Update Complete.");
        }

        protected void ProcessMessage(UnityMessage message)
        {
            if (message == null)
            {
                Debug.Log("Did not recieve valid message from Max!");
                return;
            }

            if (message is MessagePing)
            {
                Debug.Log((message as MessagePing).message);
                return;
            }

            if (message is MessageGeometryUpdate)
            {
                GeometryProcessor.ProcessMessage(message as MessageGeometryUpdate);
                return;
            }

            if (message is MessageError)
            {
                Debug.Log("Max encountered an error: " + (message as MessageError).message);
                return;
            }

            Debug.Log("Encountered unknown message type: " + message.GetType().Name);
        }

    }
}

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Messaging;
using UnityEngine;

namespace MaxUnityBridge
{
    public partial class UnityImporter
    {
        protected SimpleStreamClient pipe;
        protected BinaryFormatter formatter = new BinaryFormatter();

        private GeometryBinding GeometryCore = new GeometryBinding();
        private MaterialsCore MaterialsCore = new MaterialsCore();

        public UnityImporter()
        {
            pipe = new SimpleStreamClient(15155, Debug.Log);
        }

        protected UnityMessage ExchangeIsochronous(UnityMessage message)
        {
            try
            {
                pipe.SendMessage(message);
            }
            catch (Exception e)
            {
                Debug.Log("Could not send request: " + e.Message + e.StackTrace);
            }

            object m = pipe.ReceiveMessage();

            if (!(m is UnityMessage))
            {
                Debug.Log("Could not receive message. Received type " + m.GetType().Name);
                return null;
            }

            return m as UnityMessage;
        }

        protected void ProcessIsochronous(UnityMessage message)
        {
            Debug.Log("Beginning import");

            ProcessMessage(ExchangeIsochronous(message));

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
                GeometryCore.ProcessMessage(message as MessageGeometryUpdate);
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

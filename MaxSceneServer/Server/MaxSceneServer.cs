using Autodesk.Max;
using Messaging;

namespace MaxSceneServer
{
    public partial class MaxSceneServer
    {
        protected IGlobal _gi;

        private SimpleStreamServer _pipe;

        public MaxSceneServer(IGlobal global)
        {
            _gi = global;
        }

        public void StartServer()
        {
            _pipe = new SimpleStreamServer("MaxUnityBridge", ProcessMessage, Log.Add);
        }

        public void StopServer()
        {
        }

        protected void ProcessMessage(UnityMessage message)
        {
            if (message is MessagePing)
            {
                _pipe.SendMessage(new MessagePing("Hello from Max!"));
                return;
            }

            if (message is MessageGeometryRequest)
            {
                _pipe.SendMessage(new MessageGeometryUpdate(CreateUpdates()));
                return;
            }

            if (message is MessageMaterialRequest)
            {
                _pipe.SendMessage(new MessageMaterials(GetMaterials(message as MessageMaterialRequest)));
                return;
            }

            var error = "Recieved unsupported message type: " + message.GetType().Name;
            Log.Add(error);
            SendError(error);
        }

        protected void SendError(string msg)
        {
            _pipe.SendMessage(new MessageError(msg));
        }

    }
}
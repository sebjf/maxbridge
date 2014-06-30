using Autodesk.Max;
using Messaging;
using AsyncStream;

namespace MaxSceneServer
{
    public partial class MaxSceneServer
    {
        protected IGlobal _gi;

        private SimpleStreamConnection m_pipe;

        public MaxSceneServer(SocketStreamConnection pipe)
        {
            _gi = Autodesk.Max.GlobalInterface.Instance;
            m_pipe = new SimpleStreamConnection(pipe, ProcessMessage, Log.Add);
            Log.Add("New Client Connection.");
        }

        ~MaxSceneServer()
        {
            Log.Add("Client Disconnected.");
        }

        protected void ProcessMessage(UnityMessage message)
        {
            if (message is MessagePing)
            {
                m_pipe.SendMessage(new MessagePing("Hello from Max!"));
                return;
            }

            if (message is MessageGeometryRequest)
            {
                m_pipe.SendMessage(new MessageGeometryUpdate(CreateUpdates()));
                return;
            }

            if (message is MessageMaterialRequest)
            {
                m_pipe.SendMessage(new MessageMaterials(GetMaterials(message as MessageMaterialRequest)));
                return;
            }

            if (message is MessageMapRequest)
            {
                m_pipe.SendMessage(GetMap(message as MessageMapRequest));
            }

            var error = "Recieved unsupported message type: " + message.GetType().Name;
            Log.Add(error);
            SendError(error);
        }

        protected void SendError(string msg)
        {
            m_pipe.SendMessage(new MessageError(msg));
        }

    }
}
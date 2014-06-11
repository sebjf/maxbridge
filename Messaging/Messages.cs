using System;
using System.Collections.Generic;
using System.Linq;

namespace Messaging
{
    [Serializable]
    public class UnityMessage
    {
    }

    [Serializable]
    public class MessagePing : UnityMessage
    {
        public string message;

        public MessagePing(string msg)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessageError : UnityMessage
    {
        public string message;

        public MessageError(string msg)
        {
            message = msg;
        }
    }

    [Serializable]
    public class MessageGeometryRequest : UnityMessage
    {
    }

    [Serializable]
    public class MessageGeometryUpdate : UnityMessage
    {
        public MessageGeometryUpdate(IEnumerable<GeometryNode> updates)
        {
            this.Geometries = updates.ToArray();
        }

        public GeometryNode[] Geometries;
    }

   

}

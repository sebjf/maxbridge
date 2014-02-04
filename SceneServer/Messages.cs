using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Messages
{
    [Serializable]
    public class MaxPing
    {
        public string msg = "Hello from Max";
    }

    public enum MessageTypes
    {
        Ping,
        GeometryRequest,
    }

    [Serializable]
    public class UnityMessage
    {
        public MessageTypes MessageType;


    }
}

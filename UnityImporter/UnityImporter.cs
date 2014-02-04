using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO.Pipes;
using System.IO;
using AsyncPipes;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using Messages;

namespace MaxUnityBridge
{
    public class NamedPipeClientStreamSimple
    {
        protected string name;
        protected NamedPipeClientStream pipe;
        protected int maxMessageSize = 10000000; //Remember this is just for messaging - the geometry will be passed via shared memory
        protected byte[] buffer;

        public NamedPipeClientStreamSimple(string name)
        {
            pipe = new NamedPipeClientStream(name);
            buffer = new byte[maxMessageSize];
        }

        public void MakeConnection()
        {
            if (!pipe.IsConnected)
            {
                Debug.Log("Making connection...");
                try
                {
                    pipe.Connect(1000);
                }
                catch
                {
                    Debug.Log("Could not find Max!");
                }
            }
        }

        BinaryFormatter formatter = new BinaryFormatter();

        public void SendMessage(object message)
        {
            MakeConnection();
            formatter.Serialize(pipe, message);
            pipe.Flush();
        }

        public object ReceiveMessage()
        {
            pipe.Read(buffer, 0, maxMessageSize);
            return MessageSerializers.DeserializeObject(buffer);
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

        public void DoImport()
        {
            Debug.Log("Beginning import");


            MaxPing msg = new MaxPing();
            msg.msg = "my ping!";

            pipe.SendMessage(msg);

            Debug.Log(pipe.RecieveMessage<MaxPing>().msg);
        }

    }
}

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
    public class UnityImporter
    {
        public UnityImporter()
        {
            pipe = new NamedPipeClientStream("MaxUnityBridge");
        }

        void pipe_MessageReceived(object sender, MessageEventArgs args)
        {
            Debug.Log("Message recieved");
        }

        protected NamedPipeClientStream pipe;

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
                    return;
                }
            }
        }

        public void DoImport()
        {
            Debug.Log("Beginning import");

            MakeConnection();

            MaxPing msg = new MaxPing();
            msg.msg = "my ping!";

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(pipe, msg);

            pipe.Flush();

            byte[] b = new byte[10000];
            pipe.Read(b, 0, 10000);

            Debug.Log((MessageSerializers.DeserializeObject(b) as MaxPing).msg);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using System.IO.Pipes;
using System.IO;
using AsyncPipes;
using System.ComponentModel;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using Messages;

namespace MaxExporter
{
    public class MaxUnityExporter
    {
        protected IGlobal global;

        public MaxUnityExporter(IGlobal global)
        {
            this.global = global;
        }

        NamedPipeStreamServer pipe;

        public void StartServer()
        {
            try
            {
                pipe = new NamedPipeStreamServer("MaxUnityBridge");
                pipe.MessageReceived += new MessageEventHandler(pipe_MessageReceived);
            }
            catch
            {
                Log.Add("Could not start MaxUnityBridge");
            }
        }

        public void StopServer()
        {
            //pipe.Disconnect();
        }

        void  pipe_MessageReceived(object sender, MessageEventArgs args)
        {
            try
            {
                MaxPing p = MessageSerializers.DeserializeObject(args.Message) as MaxPing;
                Log.Add(p.msg);
                p.msg = "Hello Back";
                pipe.SendMessage(MessageSerializers.SerializeObject(p));
            }
            catch
            {
                Log.Add("Recieved message from MaxUnityBridge but could not deserialise it.");
            }
        }
    }
}

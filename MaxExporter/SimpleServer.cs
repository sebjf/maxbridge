using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsyncPipes;
using Messages;

namespace MaxExporter
{
    /* This is mostly to ensure that the messages are prepended with the length by hiding the original send methods */
    public class NamedPipeSimpleServer
    {
        NamedPipeStreamServer pipe;

        public NamedPipeSimpleServer(string Name, MessageReceiveHandler ReceiveEventHandler)
        {
            try
            {
                pipe = new NamedPipeStreamServer(Name);
                pipe.MessageReceived += new MessageEventHandler(ReceiveMessage);
                messageReceiveHandler = ReceiveEventHandler;
            }
            catch
            {
                Log.Add("Could not start MaxUnityBridge");
            }
        }

        public void SendMessage(UnityMessage message)
        {
            byte[] messageData = MessageSerializers.SerializeObject(message);
            byte[] messageLen = BitConverter.GetBytes(messageData.Length);
            pipe.SendMessage(messageLen.Concat(messageData).ToArray());
        }

        protected void ReceiveMessage(object sender, MessageEventArgs args)
        {
            Log.Add("Received message from Unity.");

            try
            {
                int messageLength = BitConverter.ToInt32(args.Message, 0);
                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(args.Message, sizeof(Int32)); //fails because we recieve back the same message just sent!!!???
                if (messageReceiveHandler != null)
                {
                    messageReceiveHandler(message);
                }
            }
            catch (Exception e)
            {
                Log.Add("Recieved message from MaxUnityBridge but could not process it: " + e.Message + e.StackTrace);
            }
        }

        public delegate void MessageReceiveHandler(UnityMessage message);
        protected MessageReceiveHandler messageReceiveHandler;
    }
}

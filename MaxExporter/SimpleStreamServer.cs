using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using AsyncStream;

namespace MaxExporter
{
    /* This is mostly to ensure that the messages are prepended with the length by hiding the original send methods */
    public class SimpleStreamServer
    {
        SocketStreamServer pipe;

        public SimpleStreamServer(string Name, MessageReceiveHandler ReceiveEventHandler)
        {
            try
            {
                pipe = new SocketStreamServer(15155);
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
                ReceiveMessagePart(args.Message);                
            }
            catch (Exception e)
            {
                Log.Add("Recieved message from MaxUnityBridge but could not process it: " + e.Message + e.StackTrace);
            }
        }

        /* We don't know how many segments the message will arrive in so build it event by event before deserialisation */
        protected void ReceiveMessagePart(byte[] MessagePart)
        {
            int partOffset = 0;

            if (MessageData == null)
            {
                //if messageData is null this is a new message
                int messageLength = BitConverter.ToInt32(MessagePart, 0);

                MessageData = new byte[messageLength];
                Received = 0;

                partOffset = 4;
            }

            int count = Math.Min(MessageData.Length - Received, MessagePart.Length - partOffset);
            Buffer.BlockCopy(MessagePart, partOffset, MessageData, Received, count);
            Received += count;

            if (Received >= MessageData.Length)
            {
                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(MessageData);
                if (messageReceiveHandler != null)
                {
                    messageReceiveHandler(message);
                }
            }
        }

        protected int Received = 0;
        protected byte[] MessageData;

        public delegate void MessageReceiveHandler(UnityMessage message);
        protected MessageReceiveHandler messageReceiveHandler;
    }
}

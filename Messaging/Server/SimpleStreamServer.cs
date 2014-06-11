using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using AsyncStream;

namespace Messaging
{
    /* This is mostly to ensure that the messages are prepended with the length by hiding the original send methods */
    public class SimpleStreamServer
    {
        private SocketStreamServer _pipe;

        protected int _received = 0;
        protected byte[] _messageData;

        public delegate void MessageReceiveHandler(UnityMessage message);
        protected MessageReceiveHandler _messageReceiveHandler;

        public delegate void LogCallback(string message);
        protected LogCallback _logCallback;

        private void Log(string message)
        {
            if (_logCallback != null)
            {
                _logCallback(message);
            }
        }

        public SimpleStreamServer(string name, MessageReceiveHandler receiveEventHandler, LogCallback logCallback)
        {
            try
            {
                _logCallback = logCallback;
                _pipe = new SocketStreamServer(15155);
                _pipe.MessageReceived += new MessageEventHandler(ReceiveMessage);
                _messageReceiveHandler = receiveEventHandler;
            }
            catch
            {
                Log("Could not start " + name);
            }
        }

        public void SendMessage(UnityMessage message)
        {
            byte[] messageData = MessageSerializers.SerializeObject(message);
            byte[] messageLen = BitConverter.GetBytes(messageData.Length);
            _pipe.SendMessage(messageLen.Concat(messageData).ToArray());
        }

        protected void ReceiveMessage(object sender, MessageEventArgs args)
        {
            try
            {
                ReceiveMessagePart(args.Message);                
            }
            catch (Exception e)
            {
                Log("Recieved message from MaxUnityBridge but could not process it: " + e.Message + e.StackTrace);
            }
        }

        /* We don't know how many segments the message will arrive in so build it event by event before deserialisation */
        protected void ReceiveMessagePart(byte[] MessagePart)
        {
            int partOffset = 0;

            if (_messageData == null)
            {
                //if messageData is null this is a new message
                int messageLength = BitConverter.ToInt32(MessagePart, 0);

                _messageData = new byte[messageLength];
                _received = 0;

                partOffset = 4;
            }

            int count = Math.Min(_messageData.Length - _received, MessagePart.Length - partOffset);
            Buffer.BlockCopy(MessagePart, partOffset, _messageData, _received, count);
            _received += count;

            if (_received >= _messageData.Length)
            {
                Log("Received message from Unity.");

                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(_messageData);
                if (_messageReceiveHandler != null)
                {
                    _messageReceiveHandler(message);
                }
                _messageData = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using AsyncStream;

namespace Messaging
{
    /* This is mostly to ensure that the messages are prepended with the length by hiding the underlying stream from the scene server. It differs from SimpleStreamClient
     * mostly in semantics, the underlying methods do the same thing but work slightly differently, with these being skewed towards asynchronous operation */
    public class SimpleStreamConnection
    {
        private SocketStreamConnection m_pipe;

        protected int m_received = 0;
        protected byte[] m_messageData;

        public delegate void MessageReceiveHandler(UnityMessage message);
        protected MessageReceiveHandler m_messageReceiveHandler;

        public delegate void LogCallback(string message);
        protected LogCallback m_logCallback;

        private void Log(string message)
        {
            if (m_logCallback != null)
            {
                m_logCallback(message);
            }
        }

        public SimpleStreamConnection(SocketStreamConnection clientSocket, MessageReceiveHandler receiveEventHandler, LogCallback logCallbackHandler)
        {
            m_logCallback = logCallbackHandler;
            m_pipe = clientSocket;
            m_pipe.MessageReceived += new MessageEventHandler(ReceiveMessage);
            m_messageReceiveHandler = receiveEventHandler;
        }

        public void SendMessage(UnityMessage message)
        {
            byte[] messageData = MessageSerializers.SerializeObject(message);
            byte[] messageLen = BitConverter.GetBytes(messageData.Length);
            m_pipe.SendMessage(messageLen.Concat(messageData).ToArray());
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

            if (m_messageData == null)
            {
                //if messageData is null this is a new message
                int messageLength = BitConverter.ToInt32(MessagePart, 0);

                m_messageData = new byte[messageLength];
                m_received = 0;

                partOffset = 4;
            }

            int count = Math.Min(m_messageData.Length - m_received, MessagePart.Length - partOffset);
            Buffer.BlockCopy(MessagePart, partOffset, m_messageData, m_received, count);
            m_received += count;

            if (m_received >= m_messageData.Length)
            {
                Log("Received message from Unity.");

                UnityMessage message = MessageSerializers.DeserializeMessage<UnityMessage>(m_messageData);
                if (m_messageReceiveHandler != null)
                {
                    m_messageReceiveHandler(message);
                }
                m_messageData = null;
            }
        }
    }
}

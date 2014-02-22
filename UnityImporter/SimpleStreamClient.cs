using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using UnityEngine;
using UnityEditor;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace MaxUnityBridge
{
    public class SimpleStreamClient
    {
        protected string name;
        protected TcpClient pipe;
        protected int port;

        public SimpleStreamClient(int port)
        {
            this.port = port;
        }

        public bool MakeConnection()
        {
            if (pipe == null)
            {
                pipe = new TcpClient("localhost", port);
            }

            if (!pipe.Connected)
            {
                try
                {
                    pipe.Connect("localhost", port);
                }
                catch
                {
                    Debug.Log("Could not find Max!");
                    return false;
                }
            }

            return true;
        }

        public void SendMessage(object message)
        {
            if (MakeConnection())
            {
                byte[] data = MessageSerializers.SerializeObject(message);
                byte[] dataLengthData = BitConverter.GetBytes((Int32)data.Length);
                pipe.GetStream().Write(dataLengthData, 0, 4);
                pipe.GetStream().Write(data, 0, data.Length);
                pipe.GetStream().Flush();
            }
        }

        public object ReceiveMessage()
        {
            try
            {
                byte[] dataLength = new byte[4];
                pipe.GetStream().Read(dataLength, 0, 4);
                int msglength = BitConverter.ToInt32(dataLength, 0);

                byte[] data = new byte[msglength];

                int read = 0;
                do{
                    read += pipe.GetStream().Read(data, read, msglength - read);
                } while (read < msglength);

                return MessageSerializers.DeserializeObject(data);
            }
            catch
            {
                Debug.Log("Could not recieve message, there may be garbage in the pipe. Closing it...");
                try
                {
                    pipe.Client.Disconnect(false);
                }
                catch { }
                pipe = null;
                return null;
            }
        }

        public T RecieveMessage<T>()
        {
            return (T)ReceiveMessage();
        }
    }
}

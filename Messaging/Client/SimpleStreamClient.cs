using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messaging;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Messaging
{
    public class SimpleStreamClient
    {
        protected string _name;
        protected TcpClient _pipe;
        protected int _port;

        public delegate void LogCallback(string message);
        protected LogCallback _logCallback;

        private void Log(string message)
        {
            if (_logCallback != null)
            {
                _logCallback(message);
            }
        }

        public SimpleStreamClient(int port, LogCallback logCallback)
        {
            _logCallback = logCallback;
            _port = port;
        }

        public bool MakeConnection()
        {
            if (_pipe == null)
            {
                _pipe = new TcpClient("localhost", _port);
            }

            if (!_pipe.Connected)
            {
                try
                {
                    _pipe.Connect("localhost", _port);
                }
                catch
                {
                    Log("Could not find Max!");
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
                _pipe.GetStream().Write(dataLengthData, 0, 4);
                _pipe.GetStream().Write(data, 0, data.Length);
                _pipe.GetStream().Flush();
            }
        }

        public object ReceiveMessage()
        {
            try
            {
                byte[] dataLength = new byte[4];
                _pipe.GetStream().Read(dataLength, 0, 4);
                int msglength = BitConverter.ToInt32(dataLength, 0);

                byte[] data = new byte[msglength];

                int read = 0;
                do{
                    read += _pipe.GetStream().Read(data, read, msglength - read);
                } while (read < msglength);

                return MessageSerializers.DeserializeObject(data);
            }
            catch(Exception e)
            {
                Log("Could not recieve message, there may be garbage in the pipe. Closing it..." + e.Message);
                try
                {
                    _pipe.Client.Disconnect(false);
                }
                catch { }
                _pipe = null;
                return null;
            }
        }

        public T RecieveMessage<T>()
        {
            return (T)ReceiveMessage();
        }
    }
}

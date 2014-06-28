using System;
using System.IO.Pipes;
using System.Net.Sockets;

namespace AsyncStream
{
    public class SocketStreamConnection : SocketStreamBase
    {
        private readonly object _InstanceLock;
        private Socket _Stream;

        public SocketStreamConnection(Socket stream)
        {
            this._InstanceLock = new object();
            this._Stream = stream;
            byte[] buffer = new byte[BUFFER_LENGTH];
            this._Stream.BeginReceive(buffer, 0, BUFFER_LENGTH, SocketFlags.None, new AsyncCallback(this.EndRead), buffer);
        }

        public override void Disconnect()
        {
            lock (this._InstanceLock)
            {
                base.Disconnect();
                this._Stream.Close();
            }
        }

        private void EndRead(IAsyncResult result)
        {
            int length = 0;
            try
            {
                length = this._Stream.EndReceive(result);
            }
            catch
            {
                //socket closed
                _Stream.Close();
                return;
            }

            byte[] asyncState = (byte[])result.AsyncState;
            if (length > 0)
            {
                byte[] destinationArray = new byte[length];
                Array.Copy(asyncState, 0, destinationArray, 0, length);
                this.OnMessageReceived(new MessageEventArgs(destinationArray));
            }

            if (length <= 0) //if EndReceive returns 0 its because the remote host has called Shutdown.
            {
                _Stream.Close();
                return;
            }

            lock (this._InstanceLock)
            {
                if (this._Stream.Connected)
                {
                    try
                    {
                        this._Stream.BeginReceive(asyncState, 0, BUFFER_LENGTH, SocketFlags.None, new AsyncCallback(this.EndRead), asyncState);
                    }
                    catch
                    {
                        //Socket Closed
                    }
                }
            }
        }

        private void EndSendMessage(IAsyncResult result)
        {
            lock (this._InstanceLock)
            {
                this._Stream.EndSend(result);
            }
        }

        ~SocketStreamConnection()
        {
            this.Dispose(false);
        }

        public override void SendMessage(byte[] message)
        {
            lock (this._InstanceLock)
            {
                if (this._Stream.Connected)
                {
                    message = message ?? new byte[0];
                    this._Stream.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(this.EndSendMessage), null);
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return this._Stream.Connected;
            }
        }
    }
}

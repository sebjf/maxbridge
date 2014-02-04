namespace AsyncPipes
{
    using System;
    using System.IO.Pipes;

    public class NamedPipeStreamConnection : NamedPipeStreamBase
    {
        private readonly object _InstanceLock;
        private PipeStream _Stream;

        public NamedPipeStreamConnection(PipeStream stream, string pipeName) : base(pipeName)
        {
            this._InstanceLock = new object();
            this._Stream = stream;
            byte[] buffer = new byte[NamedPipeStreamBase.BUFFER_LENGTH];
            this._Stream.BeginRead(buffer, 0, NamedPipeStreamBase.BUFFER_LENGTH, new AsyncCallback(this.EndRead), buffer);
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
            int length = this._Stream.EndRead(result);
            byte[] asyncState = (byte[]) result.AsyncState;
            if (length > 0)
            {
                byte[] destinationArray = new byte[length];
                Array.Copy(asyncState, 0, destinationArray, 0, length);
                this.OnMessageReceived(new MessageEventArgs(destinationArray));
            }
            lock (this._InstanceLock)
            {
                try
                {
                    this._Stream.BeginRead(asyncState, 0, NamedPipeStreamBase.BUFFER_LENGTH, new AsyncCallback(this.EndRead), asyncState);
                }
                catch (ObjectDisposedException)
                {
                    //Pipe closed
                }

            }
        }

        private void EndSendMessage(IAsyncResult result)
        {
            lock (this._InstanceLock)
            {
                this._Stream.EndWrite(result);
                this._Stream.Flush();
            }
        }

        ~NamedPipeStreamConnection()
        {
            this.Dispose(false);
        }

        public override void SendMessage(byte[] message)
        {
            lock (this._InstanceLock)
            {
                if (this._Stream.IsConnected)
                {
                    message = message ?? new byte[0];
                    this._Stream.BeginWrite(message, 0, message.Length, new AsyncCallback(this.EndSendMessage), null);
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return this._Stream.IsConnected;
            }
        }
    }
}

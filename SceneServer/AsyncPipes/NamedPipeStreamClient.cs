namespace AsyncPipes
{
    using System;
    using System.Collections.Generic;
    using System.IO.Pipes;
    using System.Threading;

    public class NamedPipeStreamClient : NamedPipeStreamBase
    {
        private ManualResetEvent _ConnectGate;
        private readonly object _InstanceLock;
        private Queue<byte[]> _PendingMessages;
        private readonly object _QueueLock;
        private PipeStream _Stream;

        public NamedPipeStreamClient(string pipeName) : base(pipeName)
        {
            this._InstanceLock = new object();
            this._QueueLock = new object();
            this._ConnectGate = new ManualResetEvent(false);
            this.StartTryConnect();
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
            // this can throw an exception when it first starts up, so...
            try
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
                    this._Stream.BeginRead(asyncState, 0, NamedPipeStreamBase.BUFFER_LENGTH,
                                           new AsyncCallback(this.EndRead), asyncState);
                }
            }
            catch
            {
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

        private void EnqueMessage(byte[] message)
        {
            lock (this._QueueLock)
            {
                if (this._PendingMessages == null)
                {
                    this._PendingMessages = new Queue<byte[]>();
                }
                this._PendingMessages.Enqueue(message);
            }
        }

        ~NamedPipeStreamClient()
        {
            this.Dispose(false);
        }

        public override void SendMessage(byte[] message)
        {
            if (this._ConnectGate.WaitOne(100))
            {
                lock (this._InstanceLock)
                {
                    if (this._Stream.IsConnected)
                    {
                        message = message ?? new byte[0];
                        this._Stream.BeginWrite(message, 0, message.Length, new AsyncCallback(this.EndSendMessage), null);
                        this._Stream.Flush();
                    }
                    else
                    {
                        this.EnqueMessage(message);
                        this.StartTryConnect();
                    }
                }
            }
            else
            {
                this.EnqueMessage(message);
            }
        }

        private void SendQueuedMessages()
        {
            lock (this._QueueLock)
            {
                if (this._PendingMessages != null)
                {
                    while (this._PendingMessages.Count > 0)
                    {
                        this.SendMessage(this._PendingMessages.Dequeue());
                    }
                    this._PendingMessages = null;
                }
            }
        }

        private void StartTryConnect()
        {
            this._ConnectGate.Reset();
            Thread thread = new Thread(new ThreadStart(this.TryConnect));
            thread.Name = "NamedPipeStreamClientConnection";
            thread.IsBackground = true;
            thread.Start();
        }

        private void TryConnect()
        {
            this._ConnectGate.Reset();
            lock (this._InstanceLock)
            {
                if (base.PipeName.Contains("\\"))
                {
                    string serverName = base.PipeName.Substring(base.PipeName.IndexOf("\\") +1, base.PipeName.LastIndexOf("\\") - 1);
                    string pipeName = base.PipeName.Substring(base.PipeName.LastIndexOf("\\") + 1);
                    this._Stream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut,
                                                             PipeOptions.Asynchronous);
                }
                else
                    this._Stream = new NamedPipeClientStream(".", base.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                while (!this._Stream.IsConnected)
                {
                    try
                    {
                        ((NamedPipeClientStream) this._Stream).Connect(0x3e8);
                    }
                    catch
                    {
                    }
                }
                this._Stream.ReadMode = PipeTransmissionMode.Message;
                byte[] buffer = new byte[NamedPipeStreamBase.BUFFER_LENGTH];
                this._Stream.BeginRead(buffer, 0, NamedPipeStreamBase.BUFFER_LENGTH, new AsyncCallback(this.EndRead), buffer);
                this._ConnectGate.Set();
                this.SendQueuedMessages();
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (this._InstanceLock)
                {
                    return this._Stream.IsConnected;
                }
            }
        }
    }
}

namespace AsyncPipes
{
    using System;

    public abstract class NamedPipeStreamBase : IDisposable
    {
        private readonly object _EventLock = new object();
        private MessageEventHandler _MessageReceived;
        private readonly string _PipeName;
        public static readonly int BUFFER_LENGTH = 65536;

        public event MessageEventHandler MessageReceived
        {
            add
            {
                lock (this._EventLock)
                {
                    this._MessageReceived = (MessageEventHandler) Delegate.Combine(this._MessageReceived, value);
                }
            }
            remove
            {
                lock (this._EventLock)
                {
                    this._MessageReceived = (MessageEventHandler) Delegate.Remove(this._MessageReceived, value);
                }
            }
        }

        protected NamedPipeStreamBase(string pipeName)
        {
            this._PipeName = pipeName;
        }

        public virtual void Disconnect()
        {
            lock (this._EventLock)
            {
                this._MessageReceived = null;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        ~NamedPipeStreamBase()
        {
            this.Dispose(false);
        }

        protected virtual void OnMessageReceived(MessageEventArgs args)
        {
            lock (this._EventLock)
            {
                if (this._MessageReceived != null)
                {
                    this._MessageReceived(this, args);
                }
            }
        }

        public abstract void SendMessage(byte[] message);

        public string PipeName
        {
            get
            {
                return this._PipeName;
            }
        }
    }
}

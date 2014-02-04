namespace AsyncPipes
{
    using System;
    using System.Collections.Generic;
    using System.IO.Pipes;

    public class NamedPipeStreamServer : NamedPipeStreamBase
    {
        private List<NamedPipeStreamConnection> _Connections;

        public NamedPipeStreamServer(string pipeName) : base(pipeName)
        {
            this._Connections = new List<NamedPipeStreamConnection>();
            NamedPipeServerStream state = new NamedPipeServerStream(base.PipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            state.BeginWaitForConnection(new AsyncCallback(this.ClientConnected), state);
        }

        private void ClientConnected(IAsyncResult result)
        {
            NamedPipeServerStream asyncState = result.AsyncState as NamedPipeServerStream;
            asyncState.EndWaitForConnection(result);
            if (asyncState.IsConnected)
            {
                NamedPipeStreamConnection item = new NamedPipeStreamConnection(asyncState, base.PipeName);
                item.MessageReceived += new MessageEventHandler(this.Connection_MessageReceived);
                lock (this._Connections)
                {
                    this._Connections.Add(item);
                }
            }
            NamedPipeServerStream state = new NamedPipeServerStream(base.PipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            state.BeginWaitForConnection(new AsyncCallback(this.ClientConnected), state);
        }

        private void Connection_MessageReceived(object sender, MessageEventArgs args)
        {
            this.OnMessageReceived(args);
        }

        public override void Disconnect()
        {
            lock (this._Connections)
            {
                foreach (NamedPipeStreamConnection connection in this._Connections)
                {
                    try
                    {
                        connection.Disconnect();
                    }
                    catch
                    {
                    }
                    this._Connections.Clear();
                }
            }
        }

        ~NamedPipeStreamServer()
        {
            this.Dispose(false);
        }

        public override void SendMessage(byte[] message)
        {
            List<NamedPipeStreamConnection> list = null;
            bool flag = false;
            lock (this._Connections)
            {
                foreach (NamedPipeStreamConnection connection in this._Connections)
                {
                    try
                    {
                        flag = !connection.IsConnected;
                        if (!flag)
                        {
                            connection.SendMessage(message);
                        }
                    }
                    catch
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        connection.Disconnect();
                        if (list == null)
                        {
                            list = new List<NamedPipeStreamConnection>();
                        }
                        list.Add(connection);
                    }
                }
                if (list != null)
                {
                    foreach (NamedPipeStreamConnection connection in list)
                    {
                        this._Connections.Remove(connection);
                    }
                }
            }
        }
    }
}

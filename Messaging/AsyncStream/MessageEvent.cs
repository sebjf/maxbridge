using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncStream
{
    public delegate void MessageEventHandler(object sender, MessageEventArgs args);

    [Serializable]
    public class MessageEventArgs
    {
        private readonly byte[] _Message;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MessageEventArgs(byte[] message)
        {
            this._Message = message;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>The message.</value>
        public byte[] Message
        {
            get
            {
                return this._Message;
            }
        }
    }
}

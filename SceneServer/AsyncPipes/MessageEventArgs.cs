


//http://www.nullskull.com/a/1433/make-your-apps-talk-to-each-other-asynchronous-named-pipes-library.aspx

namespace AsyncPipes
{
    using System;

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

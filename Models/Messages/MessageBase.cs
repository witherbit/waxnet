using System;
using WAX.Enum;
using waxnet.Internal.Proto;

namespace WAX.Models.Messages
{
    public abstract class MessageBase
    {
        internal WebMessageInfo Source { get; set; }
        public DateTime TimeStamp { get; set; }
        public long ChatId { get; set; }

        public string Text { get; set; }

        public string MessageId { get; set; }

        public bool IsIncoming { get; set; }

        public MessageStatus Status { get; set; }

        public byte[] ImageData { set; get; }
    }
}

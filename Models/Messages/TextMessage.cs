using System;
using System.Collections.Generic;
using System.Text;
using waxnet.Internal.Proto;
using waxnet.Internal.Utils;

namespace WAX.Models.Messages
{
    public sealed class TextMessage : IMessage
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public long ChatId { get; set; }
        public long? OwnerId { get; set; }
        public MessageBase ReplyMessage { get; set; }
        public bool IsForwarded { get; set; }
        public string Participant { get; set; }
    }
}

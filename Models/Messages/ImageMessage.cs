using System;
using System.Collections.Generic;
using System.Text;
using waxnet.Internal.Models;
using waxnet.Internal.Proto;
using waxnet.Internal.Utils;

namespace WAX.Models.Messages
{
    public sealed class ImageMessage : IMessage
    {
        public string Text { get; set; }
        public long ChatId { get; set; }
        public long? OwnerId { get; set; }
        public MessageBase ReplyMessage { get; set; }
        public bool IsForwarded { get; set; }
        public string Participant { get; set; }
        public byte[] Content { get; set; }

        internal UploadResponse UploadResponse { get; set; }
        internal string MimeType { 
            get 
            { 
                return "image/jpeg"; 
            } 
        }
        internal string Jid
        {
            get
            {
                if (OwnerId == null)
                {
                    return ChatId.GetId();
                }
                else
                {
                    return ChatId.GetGroupId((long)OwnerId);
                }
            }
        }
        internal ContextInfo ContextInfo
        {
            get
            {
                var ci = new ContextInfo
                {
                    IsForwarded = IsForwarded
                };
                if (ReplyMessage != null)
                {
                    ci.QuotedMessage = ReplyMessage.Source;
                    ci.StanzaId = ReplyMessage.MessageId;
                }
                if (Participant != null) ci.Participant = Participant;
                return ci;
            }
        }
    }
}

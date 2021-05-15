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
    }
}

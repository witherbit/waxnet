using System;
using System.Collections.Generic;
using System.Text;
using waxnet.Internal.Utils;

namespace WAX.Models.Messages
{
    public class ChatMessage : MessageBase
    {
        public override string ToString()
        {
            return ChatId.GetId();
        }
    }
}

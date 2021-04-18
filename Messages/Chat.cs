using System;
using System.Collections.Generic;
using System.Text;
using WAX.Enum;

namespace WAX.Messages
{
    public class Chat
    {
        public string Name { get; internal set; }

        public string ChatId { get; internal set; }

        public ChatType Type { get; internal set; }

        public Message Message { get; internal set; }
    }
}

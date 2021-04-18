using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAX.Enum;

namespace WAX.Messages
{
    public abstract class MessageBase
    {
        public DateTime TimeStamp { get; set; }
        public string ChatId { get; set; }

        public string Text { get; set; }

        public string MessageId { get; set; }

        public bool IsIncoming { get; set; }

        public int Status { get; set; }

        public ChatType Type { get; set; }
    }
}

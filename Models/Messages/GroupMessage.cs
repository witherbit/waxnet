using System;
using System.Collections.Generic;
using System.Text;
using waxnet.Internal.Utils;

namespace WAX.Models.Messages
{
    public class GroupMessage : MessageBase
    {
        public long OwnerId { get; set; }
        public override string ToString()
        {
            return ChatId.GetGroupId(OwnerId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAX.Enum
{
    public enum MessageStatus
    {
        Error,
        Pending,
        ServerAck,
        DeliveryAck,
        Read,
        Played
    }
}

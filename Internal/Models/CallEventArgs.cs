using System;
using System.Collections.Generic;
using System.Text;

namespace waxnet.Internal.Models
{
    struct CallEventArgs
    {
        public object Content { get; set; }
        public CallEventType Type { get; set; }
    }

    enum CallEventType
    {
        CodeUpdate,
        Login,
        AccountDropped,
        Exception,
        Handle,
        Message,
        GroupMessage,
        Stop,
        Start
    }
}

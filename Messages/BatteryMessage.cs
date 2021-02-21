using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Messages
{
    public struct BatteryMessage
    {
        public bool Plugged { get; internal set; }
        public int Battery { get; internal set; }
    }
}

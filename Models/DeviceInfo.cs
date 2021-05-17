using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Models
{
    public struct DeviceInfo
    {
        public int Battery { get; internal set; }
        public bool Plugged { get; internal set; }
        public bool Connect { get; internal set; }
        public string OsVersion{ get; internal set; }
        public string Manufacturer { get; internal set; }
        public string Model { get; internal set; }
        public string Platform { get; internal set; }
    }
}

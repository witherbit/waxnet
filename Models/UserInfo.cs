using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Models
{
    public struct UserInfo
    {
        public string PushName { get; internal set; }
        public long Id { get; internal set; }
        public string Version { get; internal set; }
    }
}

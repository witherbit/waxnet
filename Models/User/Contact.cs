using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Models.User
{
    public struct Contact
    {
        public long Id { get; internal set; }
        public string Name { get; internal set; }
        public string Notify { get; internal set; }
        public string Short { get; internal set; }
        public string Index { get; internal set; }
    }
}

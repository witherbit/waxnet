using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Models.User
{
    public struct ExistInfo
    {
        internal static ExistInfo Empty
        {
            get
            {
                return new ExistInfo();
            }
        }
        public bool Exist { get; internal set; }
        public int Status { get; internal set; }
        public long Id { get; internal set; }
        public bool Business { get; internal set; }

        public override string ToString()
        {
            return Exist.ToString();
        }
    }
}

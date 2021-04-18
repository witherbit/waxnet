using System;
using System.Collections.Generic;
using System.Text;
using WAX.Models;

namespace WAX.Delegates
{
    public struct Del
    {
        public delegate void QrHandler(string qr);
        public delegate void LoginHandler(Session session);
        public delegate void ReceiveHandle(ReceiveModel session);
    }
}

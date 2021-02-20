using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Messages
{
    public class ImageMessage : MessageBase
    {
        public byte[] ImageData { set; get; }
    }
}

using System;

namespace waxnet.Internal.Models
{
    class Session
    {
        public string ClientId { get; set; }

        public string ClientToken { get; set; }

        public string ServerToken { get; set; }

        public byte[] EncKey { get; set; }

        public byte[] MacKey { get; set; }

        public string Id { set; get; }
    }
}

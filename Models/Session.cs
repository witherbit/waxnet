using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAX.Models
{
    public class Session
    {
        public string ClientId { get; set; }

        public string ClientToken { get; set; }

        public string ServerToken { get; set; }

        public byte[] EncKey { get; set; }

        public byte[] MacKey { get; set; }

        public string Id { set; get; }
    }
}

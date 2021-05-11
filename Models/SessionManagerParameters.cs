using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.Models
{
    public struct SessionManagerParameters
    {
        public string FilePath { get; set; }

        public string Salt { internal get; set; }

        public string Key { internal get; set; }
    }
}

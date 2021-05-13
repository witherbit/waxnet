using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WAX;
using waxnet.Internal.Models;

namespace waxnet.Internal.Core
{
    class Handler
    {
        internal Api _api;
        internal void Controller(ReceiveModel rm)
        {
            //Console.WriteLine(rm.ToString());
        }

        internal async Task Message()
        {
            await Task.Run(()=>
            {

            });
        }

        internal async Task ConnectInfo()
        {
            await Task.Run(() =>
            {

            });
        }
    }
}

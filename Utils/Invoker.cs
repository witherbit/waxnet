using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WAX.Utils
{
    static class Invoker
    {
        public static void Wait(Action act)
        {
            while (true)
            {
                try
                {
                    act.Invoke();
                    break;
                }
                catch
                {

                }
            }
        }

        public static async Task<object> StartAsync(Func<object> act)
        {
            return await Task.Run(()=>
            {
                return act.Invoke();
            });
        }
    }
}

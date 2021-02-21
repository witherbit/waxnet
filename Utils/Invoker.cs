using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WAX.Utils
{
    static class Invoker
    {
        public static async Task<object> StartAsync(Func<object> act)
        {
            return await Task.Run(()=>
            {
                return act.Invoke();
            });
        }

        public static async void StartAsync(Action act)
        {
            await Task.Run(() =>
            {
                act.Invoke();
            });
        }
    }
}

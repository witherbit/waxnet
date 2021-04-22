using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WAX.Models;

namespace WAX.Utils
{
    static class SyncReceive
    {
        private static Dictionary<string, ReceiveModel> _receiveModels = new Dictionary<string, ReceiveModel>();
        public static async Task<ReceiveModel> WaitResult(string tag, int delay = 0)
        {
            return await Task.Run(()=>
            {
                Task.Delay(delay).Wait();
                _receiveModels.Add(tag, null);
                while (true)
                {
                    var rm = _receiveModels[tag];
                    if (rm != null)
                    {
                        _receiveModels.Remove(tag);
                        return rm;
                    }
                }
            });
        }
        public async static void Select(ReceiveModel rm)
        {
            await Task.Run(()=>
            {
                if (rm != null && rm.Tag != null && _receiveModels.ContainsKey(rm.Tag))
                    _receiveModels[rm.Tag] = rm;
            });
        }
    }
}

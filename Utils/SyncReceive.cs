using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WAX.Models;

namespace WAX.Utils
{
    static class SyncReceive
    {
        private static Dictionary<string, ReceiveModel> _receiveModels = new Dictionary<string, ReceiveModel>();
        public static async Task<ReceiveModel> GetAsync(string tag, bool delete = true)
        {
            return await Task.Run(()=>
            {
                while (true)
                {
                    var rm = _receiveModels[tag];
                    if (rm != null)
                    {
                        if (delete) _receiveModels.Remove(tag);
                        return rm;
                    }
                }
            });
        }
        public static void AddEmpty(string tag)
        {
            _receiveModels.Add(tag, null);
        }
        public async static void TrySelectAsync(ReceiveModel rm)
        {
            await Task.Run(()=>
            {
                if (rm != null && rm.Tag != null && _receiveModels.ContainsKey(rm.Tag))
                    _receiveModels[rm.Tag] = rm;
            });
        }
    }
}

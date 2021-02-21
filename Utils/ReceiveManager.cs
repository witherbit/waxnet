using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAX.Models;

namespace WAX.Utils
{
    static class ReceiveManager
    {
        public static List<ReceiveModel> receiveModels = new List<ReceiveModel>();
        public static async Task<ReceiveModel> GetReceiveAsync(string tag, bool delete = true)
        {
            return await Task.Run(()=>
            {
                while (true)
                {
                    var rm = receiveModels.FirstOrDefault(c => c.Tag == tag);
                    if (rm != null)
                    {
                        if (delete) receiveModels.Remove(rm);
                        return rm;
                    }
                }
            });
        }
    }
}

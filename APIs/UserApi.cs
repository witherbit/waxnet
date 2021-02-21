using System;
using System.Collections.Generic;
using System.Text;
using WAX.Enum;
using WAX.Models;
using WAX.Utils;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace WAX.APIs
{
    public class UserApi
    {
        private Api _api;
        public UserApi(Api api)
        {
            _api = api;
        }

        public void SetStatus(string status)
        {
            
        }

        public async Task<string> GetStatus(string userId)
        {
            return await Task.Run(() =>
            {
                var rm = _api.SendJson($"[\"query\",\"Status\",\"{userId}\"]");
                return rm.Item2.Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
            });
        }

        public async void Contacts()
        {
            var res = _api.SendQuery("contacts", "", "", "", "", "", 0, 0);
            SyncReceive.AddEmpty(res.Tag);
            res.ReceiveModel = SyncReceive.GetAsync(res.Tag).Result;
            var n = await _api.GetDecryptNode(res.ReceiveModel);
            Console.WriteLine(JsonConvert.SerializeObject(n));
        }
    }
}

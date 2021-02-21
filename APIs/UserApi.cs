using System;
using System.Collections.Generic;
using System.Text;
using WAX.Enum;
using WAX.Models;
using WAX.Utils;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

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

        public void Contacts()
        {
            var rm = _api.SendQuery("contacts", "", "", "", "", "", 0, 0, waitTime: 3000).receiveModel;
            Console.WriteLine(rm.StringData);
        }
    }
}

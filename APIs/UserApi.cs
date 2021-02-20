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

        public string GetStatus(string userId)
        {
            return Invoker.StartAsync(() =>
            {
                string s = null;
                _api.SendJson($"[\"query\",\"Status\",\"{userId}\"]", rm => s = rm.Body);
                Invoker.Wait(() => {
                    s = s.RegexGetString("\"status\":\"([^\"]*)\"");
                    s = Regex.Replace(s.Replace(@"\u200e", ""), @"\\u([\da-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                });
                return s;
            }).Result.ToString();
        }

        public void Contacts()
        {
            var tag = $"{DateTime.Now.GetTimeStampInt()}.--{_api._msgCount}";
            Action<ReceiveModel> act = new Action<ReceiveModel>((rm)=>
            {
                var i = rm.Body;
                Console.WriteLine(i);
            });
            _api.AddCallback(tag, act);
            var n = new Node
            {
                Description = "query",
                Attributes = new Dictionary<string, string> {
                    { "type", t },
                    {"epoch",msgCount.ToString() }//"5" }//
                },
            };
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WAX.Enum;
using waxnet.Internal.Consts;
using waxnet.Internal.Core;
using waxnet.Internal.Models;
using waxnet.Internal.Utils;

namespace WAX.Methods
{
    public sealed class User
    {
        internal Api _api;
        public string GetStatus(long id)
        {
            return _api.Engine.ReceiveManager.WaitResult(_api.Engine.SendJson($"[\"query\",\"Status\",\"{id.GetId()}\"]")).Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
        }
        public JToken IsExist(long id)
        {
            return JToken.Parse(_api.Engine.ReceiveManager.WaitResult(_api.Engine.SendJson($"[\"query\",\"exist\",\"{id.GetId()}\"]")).Body);
        }
        public JToken Contacts()
        {
            var rm = _api.Engine.ReceiveManager.WaitResult(_api.Engine.SendQuery("contacts", "", "", "", "", "", 0, 0), ignoreCount: 1);
            var n = _api.Engine.GetDecryptNode(rm);
            return JToken.Parse(JsonConvert.SerializeObject(n));
        }
    }
}

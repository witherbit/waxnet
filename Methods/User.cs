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

        public string GetStatus(long userId)
        {
            if (!_api.CheckLock()) return null;
            return _api.Engine.SendJsonGet($"[\"query\",\"Status\",\"{userId.GetId()}\"]").Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
        }

        public JToken IsExist(long userId)
        {
            if (!_api.CheckLock()) return null;
            return JToken.Parse(_api.Engine.SendJsonGet($"[\"query\",\"exist\",\"{userId.GetId()}\"]").Body);
        }

        public JToken Contacts()
        {
            if (!_api.CheckLock()) return null;
            var rm = _api.Engine.SendQueryGet("contacts", "", "", "", "", "", 0, 0);
            var n = _api.Engine.GetDecryptNode(rm);
            return JToken.Parse(JsonConvert.SerializeObject(n));
        }

        public JToken Chats()
        {
            if (!_api.CheckLock()) return null;
            var rm = _api.Engine.SendQueryGet("chat", "", "", "", "", "", 0, 0);
            var n = _api.Engine.GetDecryptNode(rm);
            return JToken.Parse(JsonConvert.SerializeObject(n));
        }

        public async void SetPresence(long userId, PresenceType type)
        {
            await Task.Run(() =>
            {
                if (!_api.CheckLock()) return;
                var tag = _api.Engine.Tag;
                var content = new Node()
                {
                    Description = "presence",
                    Attributes = new Dictionary<string, string>
                        {
                            { "type", PresenceConst.Presences[(int)type] }
                        }
                };
                if (type == PresenceType.Composing || type == PresenceType.Recording || type == PresenceType.Paused)
                {
                    content.Attributes.Add("to", userId.GetId());
                }
                var n = new Node()
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api.Engine._msgCount.ToString() },
                },
                    Content = new List<Node> {
                    content
                }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }

        public string SubscribePresence(string userId)
        {
            if (!_api.CheckLock()) return null;
            return _api.Engine.SendJsonGet($"[\"action\",\"presence\",\"subscribe\",\"{userId.GetId()}\"]").Body;
        }
    }
}

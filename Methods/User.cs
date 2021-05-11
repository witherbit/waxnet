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
            return _api._engine.SendJsonGet($"[\"query\",\"Status\",\"{userId.GetId()}\"]").Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
        }

        public JToken IsExist(long userId)
        {
            return JToken.Parse(_api._engine.SendJsonGet($"[\"query\",\"exist\",\"{userId.GetId()}\"]").Body);
        }

        public JToken Contacts()
        {
            var rm = _api._engine.SendQueryGet("contacts", "", "", "", "", "", 0, 0);
            var n = _api._engine.GetDecryptNode(rm);
            return JToken.Parse(JsonConvert.SerializeObject(n));
        }

        public JToken Chats()
        {
            var rm = _api._engine.SendQueryGet("chat", "", "", "", "", "", 0, 0);
            var n = _api._engine.GetDecryptNode(rm);
            return JToken.Parse(JsonConvert.SerializeObject(n));
        }

        public async void SetPresence(string chatId, PresenceType type)
        {
            await Task.Run(() =>
            {
                var tag = _api._engine.Tag;
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
                    content.Attributes.Add("to", chatId);
                }
                var n = new Node()
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api._engine._msgCount.ToString() },
                },
                    Content = new List<Node> {
                    content
                }
                };
                _api._engine.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }

        public string SubscribePresence(string chatId)
        {
            return _api._engine.SendJsonGet($"[\"action\",\"presence\",\"subscribe\",\"{chatId}\"]").Body;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using WAX.Enum;
using WAX.Models;
using WAX.Utils;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WAX.Consts;
using Newtonsoft.Json.Linq;

namespace WAX.APIs
{
    public class UserApi
    {
        private Api _api;
        public UserApi(Api api)
        {
            _api = api;
        }

        public async void SetStatus(string status)
        {
            await Task.Run(()=>
            {
                var tag = _api.GetTag();
                var n = new Node()
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api._msgCount.ToString() },
                },
                    Content = new List<Node> {
                    new Node
                    {
                        Description = "status",
                        Attributes = null,
                        Content = Encoding.UTF8.GetBytes(status)
                    }
                }
                };
                _api.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }

        public async void SetName(string name)
        {
            await Task.Run(()=>
            {
                var tag = _api.GetTag();
                var n = new Node()
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api._msgCount.ToString() },
                },
                    Content = new List<Node> {
                    new Node
                    {
                        Description = "name",
                        Attributes = null,
                        Content = Encoding.UTF8.GetBytes(name)
                    }
                }
                };
                _api.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }

        public async Task<string> GetStatus(long userId)
        {
            return await Task.Run(()=>
            {
                return _api.SendJsonGet($"[\"query\",\"Status\",\"{userId.GetId()}\"]").Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
            });
        }

        public async Task<JToken> IsExist(long userId)
        {
            return await Task.Run(()=>
            {
                return JToken.Parse(_api.SendJsonGet($"[\"query\",\"exist\",\"{userId.GetId()}\"]").Body);
            });
        }

        public async Task<JToken> Contacts()
        {
            return await Task.Run(()=>
            {
                var rm = _api.SendQueryGet("contacts", "", "", "", "", "", 0, 0, delay: 500);
                var n = _api.GetDecryptNode(rm);
                return JToken.Parse(JsonConvert.SerializeObject(n));
            });
        }

        public async Task<JToken> Chats()
        {
            return await Task.Run(() =>
            {
                var rm = _api.SendQueryGet("chat", "", "", "", "", "", 0, 0, delay:500);
                var n = _api.GetDecryptNode(rm);
                return JToken.Parse(JsonConvert.SerializeObject(n));
            });
        }

        public string GetChat(string chatId)
        {
            var tag = _api.SendQuery("search", "", "", "", "", chatId, 1, 1);
            var rm = SyncReceive.WaitResult(tag).Result;
            return rm.StringData;
        }

        public void DeleteChat(string chatId)
        {

        }

        public async void SetPresence(string chatId, PresenceType type)
        {
            await Task.Run(()=>
            {
                var tag = _api.GetTag();
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
                    { "epoch", _api._msgCount.ToString() },
                },
                    Content = new List<Node> {
                    content
                }
                };
                _api.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }

        public string SubscribePresence(string chatId)
        {
            var tag = _api.SendJson($"[\"action\",\"presence\",\"subscribe\",\"{chatId}\"]");
            return SyncReceive.WaitResult(tag).Result.Body;
        }
    }
}

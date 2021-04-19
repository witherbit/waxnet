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

        public void SetStatus(string status)
        {
            Invoker.StartAsync(()=>
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

        public void SetName(string name)
        {
            Invoker.StartAsync(() =>
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

        public string GetStatus(long userId)
        {
            return Invoker.StartAsync(()=>
            {
                var tag = _api.SendJson($"[\"query\",\"Status\",\"{userId.GetId()}\"]");
                return SyncReceive.WaitResult(tag).Result.Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
            }).Result.ToString();
        }

        public string GetName(long userId)
        {
            return Invoker.StartAsync(() =>
            {
                var tag = _api.GetTag();
                var n = new Node()
                {
                    Description = "query",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api._msgCount.ToString() },
                },
                    Content = new List<Node> {
                }
                };
                _api.SendBinary(n, WriteBinaryType.Profile, tag);
                return SyncReceive.WaitResult(tag).Result;
            }).Result.ToString();
        }

        public bool IsExist(long userId)
        {
            var tag = _api.SendJson($"[\"query\",\"exist\",\"{userId.GetId()}\"]");
            var body = SyncReceive.WaitResult(tag).Result.Body;
            if (body.Contains("\"status\":404")) return false;
            return true;
        }

        public void Contacts()
        {
            var tag = _api.SendQuery("contacts", "", "", "", "", "", 0, 0);
            var rm = SyncReceive.WaitResult(tag, 500).Result;
            var n = _api.GetDecryptNode(rm);
            Console.WriteLine(JsonConvert.SerializeObject(n));
        }

        public void Chats()
        {
            var tag = _api.SendQuery("chat", "", "", "", "", "", 0, 0);
            var rm = SyncReceive.WaitResult(tag, 500).Result;
            var n = _api.GetDecryptNode(rm);
            Console.WriteLine(JsonConvert.SerializeObject(n));
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

        public void SetPresence(string chatId, PresenceType type)
        {
            Invoker.StartAsync(() =>
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

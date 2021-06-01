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
using WAX.Models.User;

namespace WAX.Methods
{
    public sealed class User
    {
        internal Api _api;
        public string GetStatus(long id)
        {
            if (!_api.CheckLock()) return null;
            if (_api.CheckLicense(true))
                return null;
            return _api.Engine.ReceiveManager.WaitResult(_api.Engine.SendJson($"[\"query\",\"Status\",\"{id.GetId()}\"]")).Body.RegexGetString("\"status\":\"([^\"]*)\"").ConverFromUnicode();
        }
        public ExistInfo IsExist(long id)
        {
            if (!_api.CheckLock()) return ExistInfo.Empty;
            if (_api.CheckLicense(true))
                return ExistInfo.Empty;
            var json = JToken.Parse(_api.Engine.ReceiveManager.WaitResult(_api.Engine.SendJson($"[\"query\",\"exist\",\"{id.GetId()}\"]")).Body);
            var info = new ExistInfo
            {
                Status = int.Parse(json["status"].ToString()),
                Exist = false
            };
            if(info.Status == 200)
            {
                info.Id = json["jid"].ToString().GetId();
                info.Exist = true;
                info.Business = bool.Parse(json["biz"].ToString());
            }
            return info;
        }
        public List<Contact> Contacts()
        {
            if (!_api.CheckLock()) return null;
            if (_api.CheckLicense())
                return null;
            var list = new List<Contact>();
            var n = _api.Engine.ReceiveManager.WaitResult(_api.Engine.SendQuery("contacts", "", "", "", "", "", 0, 0), ignoreCount: 1).WaitDecryptNode(_api).Result;
            if (n.Content is List<Node> nodeList)
                foreach (var node in nodeList)
                {
                    try
                    {
                        var contact = new Contact
                        {
                            Id = node.Attributes["jid"].GetId(),
                            Notify = node.Attributes["notify"]
                        };
                        if (node.Attributes.ContainsKey("short"))
                            contact.Short = node.Attributes["short"];
                        if (node.Attributes.ContainsKey("name"))
                            contact.Name = node.Attributes["name"];
                        if (node.Attributes.ContainsKey("index"))
                            contact.Index = node.Attributes["index"];
                        list.Add(contact);
                    }
                    catch { }
                }
            else return Contacts();
            return list;
        }
        public JToken Chats()
        {
            if (!_api.CheckLock()) return null;
            if (_api.CheckLicense())
                return null;
            var rm = _api.Engine.ReceiveManager.WaitResult(_api.Engine.SendQuery("chat", "", "", "", "", "", 0, 0), ignoreCount: 1);
            var n = _api.Engine.GetDecryptNode(rm);
            return JToken.Parse(JsonConvert.SerializeObject(n));
        }
        public async void SetPresence(long id, PresenceType type)
        {
            await Task.Run(() =>
            {
                if (!_api.CheckLock()) return;
                if (_api.CheckLicense(true))
                    return;
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
                    content.Attributes.Add("to", id.GetId());
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
        public async void SubscribePresence(long id)
        {
            await Task.Run(()=>
            {
                if (!_api.CheckLock()) return;
                if (_api.CheckLicense(true))
                    return;
                _api.Engine.SendJson($"[\"action\",\"presence\",\"subscribe\",\"{id.GetId()}\"]");
            });
        }
        public async void UnsubscribePresence(long id)
        {
            await Task.Run(() =>
            {
                if (!_api.CheckLock()) return;
                if (_api.CheckLicense(true))
                    return;
                _api.Engine.SendJson($"[\"action\",\"presence\",\"unsubscribe\",\"{id.GetId()}\"]");
            });
        }
        public async void Block(long id)
        {
            await Task.Run(()=>
            {
                if (!_api.CheckLock()) return;
                if (_api.CheckLicense(true))
                    return;
                var tag = _api.Engine.Tag;
                var n = new Node
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string>
                    {
                        { "type", "set" },
                        { "epoch", _api.Engine._msgCount.ToString() }
                    },
                    Content = new List<Node>
                    {
                        new Node
                        {
                            Description = "block",
                            Attributes = new Dictionary<string, string>
                            {
                                { "type", "add" }
                            },
                            Content = new Node
                            {
                                Description = "user",
                                Attributes = new Dictionary<string, string>
                                {
                                    { "jid", id + "@c.us" }
                                },
                                Content = null
                            }
                        }
                    }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Block, tag);
            });
        }
        public async void Unblock(long id)
        {
            await Task.Run(() =>
            {
                if (!_api.CheckLock()) return;
                if (_api.CheckLicense(true))
                    return;
                var tag = _api.Engine.Tag;
                var n = new Node
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string>
                    {
                        { "type", "set" },
                        { "epoch", _api.Engine._msgCount.ToString() }
                    },
                    Content = new List<Node>
                    {
                        new Node
                        {
                            Description = "block",
                            Attributes = new Dictionary<string, string>
                            {
                                { "type", "remove" }
                            },
                            Content = new Node
                            {
                                Description = "user",
                                Attributes = new Dictionary<string, string>
                                {
                                    { "jid", id + "@c.us" }
                                },
                                Content = null
                            }
                        }
                    }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Block, tag);
            });
        }
    }
}

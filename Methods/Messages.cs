using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WAX.Enum;
using waxnet.Internal.Core;
using waxnet.Internal.Models;
using waxnet.Internal.Utils;
using WAX.Models.Messages;
using Newtonsoft.Json;

namespace WAX.Methods
{
    public sealed class Messages
    {
        internal Api _api;
        public async void Send(IMessage message)
        {
            await Task.Run(()=>
            {
                if (!_api.CheckLock()) return;
                var proto = message.GetProto(_api);
                if(proto == null)
                {
                    Api.CallException(_api, new Exception("Couldn't send message"));
                    return;
                }
                try
                {
                    var node = _api.Engine.GetDecryptNode(_api.Engine.ReceiveManager.WaitResult(_api.Engine.SendProto(proto), ignoreCount: 1));
                    Console.WriteLine(JsonConvert.SerializeObject(node));
                }
                catch (Exception e)
                {
                    Api.CallException(_api, e);
                }
            });
        }
        public void Delete(MessageBase message)
        {
            if (!_api.CheckLock()) return;
            try
            {
                var tag = _api.Engine.Tag;
                string owner = message.IsIncoming ? "false" : "true";
                var n = new Node
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "epoch", _api.Engine._msgCount.ToString() },
                    { "type", "set" },
                },
                    Content = new List<Node>
                {
                    new Node {
                    Description = "chat",
                    Attributes = new Dictionary<string, string>
                    {
                        { "type", "clear"},
                        { "jid", message.Source.Key.RemoteJid },
                        { "media", "true"}
                    },
                    Content =  new List<Node>
                    {
                        new Node {
                            Description = "item",
                            Attributes = new Dictionary<string, string>
                            {
                                { "owner", owner },
                                { "index", message.MessageId }
                            }
                        }
                    }
                }
                }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Chat, tag);
            }
            catch
            {
                Api.CallException(_api, new Exception("The instance of the message object is invalid"));
            }
        }
        public void Read(MessageBase message)
        {
            if (!_api.CheckLock()) return;
            try
            {
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
                    new Node {
                        Description = "read",
                        Attributes = new Dictionary<string, string>
                        {
                            { "count", "1" },
                            { "index", message.MessageId },
                            { "jid", message.Source.Key.RemoteJid },
                            { "owner", "false" }
                        }
                    }
                }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Group, tag);
            }
            catch
            {
                Api.CallException(_api, new Exception("The instance of the message object is invalid"));
            }
        }
    }
}

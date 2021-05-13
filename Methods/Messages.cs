using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WAX;
using WAX.Enum;
using waxnet.Internal.Consts;
using waxnet.Internal.Core;
using waxnet.Internal.Models;
using waxnet.Internal.Proto;
using waxnet.Internal.Utils;
using WAX.Models.Messages;

namespace WAX.Methods
{
    public sealed class Messages
    {
        internal Api _api;
        public async void Send(Models.Messages.IMessage message)
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
                _api.Engine.SendProto(proto);
            });
        }
        public void Delete(string chatId, string messageId, bool isIncoming)
        {
            var tag = _api.Engine.Tag;
            string owner = isIncoming ? "false" : "true";
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
                        { "jid", chatId },
                        { "media", "true"}
                    },
                    Content =  new List<Node>
                    {
                        new Node {
                            Description = "item",
                            Attributes = new Dictionary<string, string>
                            {
                                { "owner", owner },
                                { "index", messageId }
                            }
                        }
                    }
                }
                }
            };
            _api.Engine.SendBinary(n, WriteBinaryType.Chat, tag);
        }

        public void Read(MessageBase message)
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
                            { "jid", message.ChatId.GetId() },
                            { "owner", "false" }
                        }
                    }
                }
            };
            _api.Engine.SendBinary(n, WriteBinaryType.Group, tag);
        }
    }
}

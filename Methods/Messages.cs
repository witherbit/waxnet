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
        public MessageBase Send(IMessage message)
        {
            if (!_api.CheckLock()) return null;
            if (_api.Engine.ServiceKeyManager.Info.StatusCode == StatusCode.OK || _api.Engine.ServiceKeyManager.Info.StatusCode == StatusCode.OKTrial)
            {
                Api.CallException(this, new Exception("Invalid licence"));
                return null;
            }
            var proto = message.GetProto(_api);
            if (proto == null)
            {
                Api.CallException(_api, new Exception("Couldn't send message"));
                return null;
            }
            var id = _api.Engine.SendProto(proto);
            if (message.OwnerId == null)
            {
                var cm = new ChatMessage
                {
                    Source = proto,
                    TimeStamp = DateTime.Now,
                    MessageId = id,
                    IsIncoming = false,
                    Status = MessageStatus.DeliveryAck,
                    ChatId = message.ChatId,
                    Text = proto.Message.Conversation,
                };
                if (message is ImageMessage)
                {
                    cm.Text = proto.Message.ImageMessage.Caption;
                    cm.ImageData = (message as ImageMessage).Content;
                }
                else if (message is VideoMessage) cm.Text = proto.Message.VideoMessage.Caption;
                return cm;
            }
            else
            {
                var gm = new GroupMessage
                {
                    Source = proto,
                    TimeStamp = DateTime.Now,
                    MessageId = id,
                    IsIncoming = false,
                    Status = MessageStatus.DeliveryAck,
                    ChatId = message.ChatId,
                    OwnerId = (long)message.OwnerId,
                    Text = proto.Message.Conversation
                };
                if (message is ImageMessage)
                {
                    gm.Text = proto.Message.ImageMessage.Caption;
                    gm.ImageData = (message as ImageMessage).Content;
                }
                else if (message is VideoMessage) gm.Text = proto.Message.VideoMessage.Caption;
                return gm;
            }
        }
        public void Delete(MessageBase message, bool forEveryone = false)
        {
            if (!_api.CheckLock()) return;
            if (_api.Engine.ServiceKeyManager.Info.StatusCode == StatusCode.OK)
            {
                Api.CallException(this, new Exception("Invalid licence"));
                return;
            }
            try
            {
                var tag = _api.Engine.Tag;
                if (!forEveryone)
                {
                    var n = new Node
                    {
                        Description = "action",
                        Attributes = new Dictionary<string, string>
                        {
                            { "epoch", _api.Engine._msgCount.ToString() },
                            { "type", "set" },
                        },
                        Content = new List<Node>
                        {
                            new Node 
                            {
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
                                            { "owner", message.IsIncoming ? "false" : "true" },
                                            { "index", message.MessageId }
                                        }
                                    }
                                }
                            }
                        }
                    };
                    _api.Engine.SendBinary(n, WriteBinaryType.Chat, tag);
                }
                else
                {
                    var receive = _api.Engine.SendProto(new waxnet.Internal.Proto.WebMessageInfo
                    {
                        Key = new waxnet.Internal.Proto.MessageKey
                        {
                            FromMe = !message.IsIncoming,
                            Id = message.MessageId,
                            RemoteJid = message.Source.Key.RemoteJid
                        },
                        MessageTimestamp = (ulong)message.TimeStamp.GetTimeStampLong(),
                        Message = new waxnet.Internal.Proto.Message
                        {
                            ProtocolMessage = new waxnet.Internal.Proto.ProtocolMessage
                            {
                                Type = waxnet.Internal.Proto.ProtocolMessage.Types.PROTOCOL_MESSAGE_TYPE.Revoke,
                                Key = new waxnet.Internal.Proto.MessageKey
                                {
                                    FromMe = true,
                                    Id = message.MessageId,
                                    RemoteJid = message.Source.Key.RemoteJid
                                }
                            }
                        },
                        Status = waxnet.Internal.Proto.WebMessageInfo.Types.WEB_MESSAGE_INFO_STATUS.Pending
                    });
                }
            }
            catch
            {
                Api.CallException(_api, new Exception("The instance of the message object is invalid"));
            }
        }
        public void Read(MessageBase message)
        {
            if (!_api.CheckLock()) return;
            if (_api.Engine.ServiceKeyManager.Info.StatusCode == StatusCode.OK)
            {
                Api.CallException(this, new Exception("Invalid licence"));
                return;
            }
            try
            {
                if (!message.IsIncoming)
                {
                    Api.CallException(_api, new Exception("You can't send a notification about reading your own message"));
                }
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

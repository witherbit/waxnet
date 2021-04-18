using Google.Protobuf;
using Proto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WAX.Consts;
using WAX.Enum;
using WAX.Models;
using WAX.Utils;
using ImageMessage = Proto.ImageMessage;

namespace WAX.APIs
{
    public class MessageApi
    {
        private Api _api;
        public MessageApi(Api api)
        {
            _api = api;
        }

        public async Task<string> SendImage(string chatId, byte[] data, string caption = null)
        {
            var uploadResponse = await _api.Upload(data, MediaTypeConst.MediaImage);
            if (uploadResponse == null)
            {
                return null;
            }
            return _api.SendProto(new WebMessageInfo()
            {
                Key = new MessageKey
                {
                    RemoteJid = chatId
                },
                Message = new Proto.Message
                {
                    ImageMessage = new ImageMessage
                    {
                        Url = uploadResponse.DownloadUrl,
                        Caption = caption,
                        Mimetype = "image/jpeg",
                        MediaKey = ByteString.CopyFrom(uploadResponse.MediaKey),
                        FileEncSha256 = ByteString.CopyFrom(uploadResponse.FileEncSha256),
                        FileSha256 = ByteString.CopyFrom(uploadResponse.FileSha256),
                        FileLength = uploadResponse.FileLength
                    }
                }
            });
        }
        public string Send(string chatId, string text, Action<ReceiveModel> act = null)
        {
            return _api.SendProto(new WebMessageInfo()
            {
                Key = new MessageKey
                {
                    RemoteJid = chatId
                },
                Message = new Proto.Message
                {
                    Conversation = text
                },
                
                
            });
        }
        public void Delete(string chatId, string messageId, bool isIncoming)
        {
            var tag = _api.GetTag();
            string owner = isIncoming ? "false" : "true";
            var n = new Node
            {
                Description = "action",
                Attributes = new Dictionary<string, string> {
                    { "epoch", _api._msgCount.ToString() },
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
            _api.SendBinary(n, WriteBinaryType.Chat, tag);
        }

        public void Read(string chatId, string messageId)
        {
            var tag = _api.GetTag();
            var n = new Node
            {
                Description = "action",
                Attributes = new Dictionary<string, string>
                {
                    { "type", "set" },
                    { "epoch", _api._msgCount.ToString() }
                },
                Content = new List<Node> 
                { 
                    new Node {
                        Description = "read",
                        Attributes = new Dictionary<string, string>
                        {
                            { "count", "1" },
                            { "index", messageId },
                            { "jid", chatId},
                            { "owner", "false" }
                        }
                    }
                }
            };
            _api.SendBinary(n, WriteBinaryType.Group, tag);
        }
    }
}

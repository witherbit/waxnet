using System;
using System.Collections.Generic;
using System.Text;
using WAX.Models;
using System.Threading;
using System.Net.WebSockets;
using Proto;
using WAX.Enum;

namespace WAX.Utils
{
    static class Write
    {
        public static (string tag, ReceiveModel receiveModel) SendProto(this Api api, WebMessageInfo webMessage)
        {
            if (webMessage.Key.Id.IsNullOrWhiteSpace())
            {
                webMessage.Key.Id = Rand.GetRandomByte(10).ToHexString().ToUpper();
            }
            if (webMessage.MessageTimestamp == 0)
            {
                webMessage.MessageTimestamp = (ulong)DateTime.Now.GetTimeStampInt();
            }
            webMessage.Key.FromMe = true;
            webMessage.Status = WebMessageInfo.Types.WEB_MESSAGE_INFO_STATUS.Error;
            var n = new Node
            {
                Description = "action",
                Attributes = new Dictionary<string, string> {
                    { "type", "relay" },
                    {"epoch",( Interlocked.Increment(ref api._msgCount) - 1).ToString() }//"5" }//
                },
                Content = new List<WebMessageInfo> { webMessage }
            };
            SendBinary(api, n, WriteBinaryType.Message, webMessage.Key.Id);
            return (webMessage.Key.Id, api.AddCallback(webMessage.Key.Id).Result);
        }
        public static void SendBinary(this Api api, Node node, WriteBinaryType binaryType, string messageTag)
        {
            var data = api.EncryptBinaryMessage(node);
            var bs = new List<byte>(Encoding.UTF8.GetBytes($"{messageTag},"));
            bs.Add((byte)binaryType);
            bs.Add(128);
            bs.AddRange(data);
            api._socket.SendAsync(new ArraySegment<byte>(bs.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        public static (string tag, ReceiveModel receiveModel) SendQuery(this Api api, string t, string jid, string messageId, string kind, string owner, string search, int count, int page, int removeCount = 0, int waitTime = 0)
        {
            var msgCount = Interlocked.Increment(ref api._msgCount) - 1;
            var tag = $"{DateTime.Now.GetTimeStampInt()}.--{msgCount}";
            var n = new Node
            {
                Description = "query",
                Attributes = new Dictionary<string, string> {
                    { "type", t },
                    {"epoch",msgCount.ToString() }
                },
            };
            if (!jid.IsNullOrWhiteSpace())
            {
                n.Attributes.Add("jid", jid);
            }
            if (!messageId.IsNullOrWhiteSpace())
            {
                n.Attributes.Add("index", messageId);
            }
            if (!kind.IsNullOrWhiteSpace())
            {
                n.Attributes.Add("kind", kind);
            }
            if (!owner.IsNullOrWhiteSpace())
            {
                n.Attributes.Add("owner", owner);
            }
            if (!search.IsNullOrWhiteSpace())
            {
                n.Attributes.Add("search", search);
            }
            if (count > 0)
            {
                n.Attributes.Add("count", count.ToString());
            }
            if (page > 0)
            {
                n.Attributes.Add("page", page.ToString());
            }
            var msgType = WriteBinaryType.Group;
            if (t == "media")
            {
                msgType = WriteBinaryType.QueryMedia;
            }
            SendBinary(api, n, msgType, tag);
            return (tag, api.AddCallback(tag, removeCount, waitTime).Result);
        }
        public static (string tag, ReceiveModel receiveModel) SendJson(this Api api, string str, int waitTime = 0)
        {
            var tag = $"{DateTime.Now.GetTimeStampInt()}.--{Interlocked.Increment(ref api._msgCount) - 1}";
            Send(api, $"{tag},{str}");
            return (tag, api.AddCallback(tag, waitTime: waitTime).Result);
        }
        public static void Send(this Api api, string str)
        {
            Send(api, Encoding.UTF8.GetBytes(str));
        }
        public static void Send(this Api api, byte[] bs)
        {
            api._socket.SendAsync(new ReadOnlyMemory<byte>(bs, 0, bs.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

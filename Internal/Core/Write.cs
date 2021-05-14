using System;
using System.Collections.Generic;
using System.Text;
using WAX.Models;
using System.Threading;
using System.Net.WebSockets;
using WAX.Enum;
using waxnet.Internal.Proto;
using waxnet.Internal.Models;
using waxnet.Internal.Utils;

namespace waxnet.Internal.Core
{
    static class Write
    {
        public static string SendProto(this Engine engine, WebMessageInfo webMessage, Action<ReceiveModel> action = null)
        {
            if (webMessage.Key.Id.IsNullOrWhiteSpace())
            {
                webMessage.Key.Id = 10.GetRandomByte().ToHexString().ToUpper();
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
                    {"epoch",( Interlocked.Increment(ref engine._msgCount) - 1).ToString() }
                },
                Content = new List<WebMessageInfo> { webMessage }
            };
            engine.AddCallback(webMessage.Key.Id, action);
            SendBinary(engine, n, WriteBinaryType.Message, webMessage.Key.Id);
            return webMessage.Key.Id;
        }
        public static void SendBinary(this Engine engine, Node node, WriteBinaryType binaryType, string tag)
        {
            var data = engine.EncryptBinaryMessage(node);
            var bs = new List<byte>(Encoding.UTF8.GetBytes($"{tag},"));
            bs.Add((byte)binaryType);
            bs.Add(128);
            bs.AddRange(data);
            engine._socket.SendAsync(new ArraySegment<byte>(bs.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        public static string SendQuery(this Engine engine, string t, string jid, string messageId, string kind, string owner, string search, int count, int page, int removeCount = 0, Action<ReceiveModel> action = null)
        {
            var msgCount = Interlocked.Increment(ref engine._msgCount) - 1;
            var tag = engine.Tag;
            engine.AddCallback(tag, action, removeCount);
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
            SendBinary(engine, n, msgType, tag);
            return tag;
        }
        public static string SendJson(this Engine engine, string json, Action<ReceiveModel> action = null)
        {
            var tag = engine.Tag;
            engine.AddCallback(tag, action);
            Send(engine, $"{tag},{json}");
            return tag;
        }
        public static void Send(this Engine engine, string str)
        {
            Send(engine, Encoding.UTF8.GetBytes(str));
        }
        public static void Send(this Engine engine, byte[] bs)
        {
            engine._socket.SendAsync(new ArraySegment<byte>(bs, 0, bs.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

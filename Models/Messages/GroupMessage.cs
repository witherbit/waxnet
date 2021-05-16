using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WAX.Enum;
using waxnet.Internal.Proto;
using waxnet.Internal.Utils;

namespace WAX.Models.Messages
{
    public class GroupMessage : MessageBase
    {
        internal static GroupMessage Build(WebMessageInfo source, Api api)
        {
            var jid = source.Key.RemoteJid.GetGroupId();
            var gm = new GroupMessage
            {
                Source = source,
                TimeStamp = source.MessageTimestamp.ToString().GetDateTime(),
                ChatId = jid.ChatId,
                OwnerId = jid.OwnerId,
                IsIncoming = !source.Key.FromMe,
                Status = (MessageStatus)source.Status,
                MessageId = source.Key.Id,
                Text = source.Message.Conversation
            };
            if (source.Message.ImageMessage != null)
            {
                try
                {
                    var fileData = api.Engine.DownloadImage(source.Message.ImageMessage.Url, source.Message.ImageMessage.MediaKey.ToArray()).Result;
                    gm.ImageData = fileData;
                    gm.Text = source.Message.ImageMessage.Caption;
                }
                catch
                {
                    try
                    {
                        api.Engine.LoadMediaInfo(source.Key.RemoteJid, source.Key.Id, source.Key.FromMe ? "true" : "false");
                        var fileData = api.Engine.DownloadImage(source.Message.ImageMessage.Url, source.Message.ImageMessage.MediaKey.ToArray()).Result;
                        gm.ImageData = fileData;
                        gm.Text = source.Message.ImageMessage.Caption;
                    }
                    catch (Exception e)
                    {
                        Api.CallException(api, e);
                    }
                }
            }
            return gm;
        }
        public long OwnerId { get; set; }
        public override string ToString()
        {
            return ChatId.GetGroupId(OwnerId);
        }
    }
}

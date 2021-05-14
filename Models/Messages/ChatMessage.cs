using waxnet.Internal.Proto;
using waxnet.Internal.Utils;
using WAX.Enum;
using System.Linq;
using System;

namespace WAX.Models.Messages
{
    public class ChatMessage : MessageBase
    {
        internal static ChatMessage Build(WebMessageInfo source, Api api)
        {
            var cm = new ChatMessage
            {
                Source = source,
                TimeStamp = source.MessageTimestamp.ToString().GetDateTime(),
                ChatId = source.Key.RemoteJid.GetId(),
                IsIncoming = !source.Key.FromMe,
                Status = (MessageStatus)source.Status,
                MessageId = source.Key.Id,
                Text = source.Message.Conversation
            };
            if(source.Message.ImageMessage != null)
            {
                try
                {
                    var fileData = api.Engine.DownloadImage(source.Message.ImageMessage.Url, source.Message.ImageMessage.MediaKey.ToArray()).Result;
                    cm.ImageData = fileData;
                    cm.Text = source.Message.ImageMessage.Caption;
                }
                catch
                {
                    try
                    {
                        api.Engine.LoadMediaInfo(source.Key.RemoteJid, source.Key.Id, source.Key.FromMe ? "true" : "false");
                        var fileData = api.Engine.DownloadImage(source.Message.ImageMessage.Url, source.Message.ImageMessage.MediaKey.ToArray()).Result;
                        cm.ImageData = fileData;
                        cm.Text = source.Message.ImageMessage.Caption;
                    }
                    catch (Exception e)
                    {
                        Api.CallException(api, e);
                    }
                }
            }
            return cm;
        }
        public override string ToString()
        {
            return ChatId.GetId();
        }
    }
}

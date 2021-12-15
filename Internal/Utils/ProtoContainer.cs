using System;
using System.Collections.Generic;
using System.Text;
using WAX.Enum;
using waxnet.Internal.Consts;
using WAX;
using WAX.Models.Parameters;
using Google.Protobuf;
using waxnet.Internal.Proto;

namespace waxnet.Internal.Utils
{
    static class ProtoContainer
    {
        public static WebMessageInfo GetProto(this WAX.Models.Messages.IMessage mp, Api api)
        {
            if (mp is WAX.Models.Messages.TextMessage) return Text(mp as WAX.Models.Messages.TextMessage);
            else if (mp is WAX.Models.Messages.ImageMessage) return Image(mp as WAX.Models.Messages.ImageMessage, api);
            else if (mp is WAX.Models.Messages.VideoMessage) return Video(mp as WAX.Models.Messages.VideoMessage, api);
            else if (mp is WAX.Models.Messages.AudioMessage) return Audio(mp as WAX.Models.Messages.AudioMessage, api);
            else
            {
                Api.CallException(mp, new Exception("The message could not be sent because the message type was not specified."));
                return null;
            }
        }

        private static WebMessageInfo Text(WAX.Models.Messages.TextMessage mp)
        {
            var etm = new ExtendedTextMessage
            {
                Text = mp.Text,
                ContextInfo = (mp as WAX.Models.Messages.IMessage).ContextInfo
            };
            if (mp.Title != null) etm.Title = mp.Title;
            return new WebMessageInfo
            {
                Key = new MessageKey
                {
                    RemoteJid = (mp as WAX.Models.Messages.IMessage).Jid
                },
                Message = new Message
                {
                    ExtendedTextMessage = etm
                },
                Status = WebMessageInfo.Types.WEB_MESSAGE_INFO_STATUS.Pending
            };
        }
        private static WebMessageInfo Image(WAX.Models.Messages.ImageMessage mp, Api api)
        {
            mp.UploadResponse = api.Engine.Upload(mp.Content, MediaType.Image).Result;
            if (mp.UploadResponse == null) return null;
            return new WebMessageInfo
            {
                Key = new MessageKey
                {
                    RemoteJid = (mp as WAX.Models.Messages.IMessage).Jid
                },
                Message = new Message
                {
                    ImageMessage = new ImageMessage
                    {
                        Url = mp.UploadResponse.DownloadUrl,
                        Caption = mp.Text,
                        Mimetype = mp.MimeType,
                        MediaKey = ByteString.CopyFrom(mp.UploadResponse.MediaKey),
                        FileEncSha256 = ByteString.CopyFrom(mp.UploadResponse.FileEncSha256),
                        FileSha256 = ByteString.CopyFrom(mp.UploadResponse.FileSha256),
                        FileLength = mp.UploadResponse.FileLength,
                        ContextInfo = (mp as WAX.Models.Messages.IMessage).ContextInfo
                    }
                },
                Status = WebMessageInfo.Types.WEB_MESSAGE_INFO_STATUS.Pending
            };
        }
        private static WebMessageInfo Video(WAX.Models.Messages.VideoMessage mp, Api api)
        {
            mp.UploadResponse = api.Engine.Upload(mp.Content, MediaType.Video).Result;
            if (mp.UploadResponse == null) return null;
            return new WebMessageInfo
            {
                Key = new MessageKey
                {
                    RemoteJid = (mp as WAX.Models.Messages.IMessage).Jid
                },
                Message = new Message
                {
                    VideoMessage = new VideoMessage
                    {
                        Url = mp.UploadResponse.DownloadUrl,
                        Caption = mp.Text,
                        Mimetype = mp.MimeType,
                        MediaKey = ByteString.CopyFrom(mp.UploadResponse.MediaKey),
                        FileEncSha256 = ByteString.CopyFrom(mp.UploadResponse.FileEncSha256),
                        FileSha256 = ByteString.CopyFrom(mp.UploadResponse.FileSha256),
                        FileLength = mp.UploadResponse.FileLength,
                        ContextInfo = (mp as WAX.Models.Messages.IMessage).ContextInfo,
                        JpegThumbnail = ByteString.CopyFrom(mp.JpegThumbnail),
                        GifPlayback = mp.GifPlayback,
                        Seconds = mp.Seconds
                    }
                },
                Status = WebMessageInfo.Types.WEB_MESSAGE_INFO_STATUS.Pending
            };
        }
        private static WebMessageInfo Audio(WAX.Models.Messages.AudioMessage mp, Api api)
        {
            mp.UploadResponse = api.Engine.Upload(mp.Content, MediaType.Audio).Result;
            if (mp.UploadResponse == null) return null;
            return new WebMessageInfo
            {
                Key = new MessageKey
                {
                    RemoteJid = (mp as WAX.Models.Messages.IMessage).Jid
                },
                Message = new Message
                {
                    AudioMessage = new AudioMessage
                    {
                        Url = mp.UploadResponse.DownloadUrl,
                        Mimetype = mp.MimeType,
                        MediaKey = ByteString.CopyFrom(mp.UploadResponse.MediaKey),
                        FileEncSha256 = ByteString.CopyFrom(mp.UploadResponse.FileEncSha256),
                        FileSha256 = ByteString.CopyFrom(mp.UploadResponse.FileSha256),
                        FileLength = mp.UploadResponse.FileLength,
                        ContextInfo = (mp as WAX.Models.Messages.IMessage).ContextInfo,
                        Ptt = mp.Ptt,
                        Seconds = mp.Seconds
                    }
                },
                Status = WebMessageInfo.Types.WEB_MESSAGE_INFO_STATUS.Pending
            };
        }
    }
}

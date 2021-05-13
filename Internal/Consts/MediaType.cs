using System;
using System.Collections.Generic;
using System.Text;

namespace waxnet.Internal.Consts
{
    struct MediaType
    {
        public const string Image = "WhatsApp Image Keys";
        public const string Video = "WhatsApp Video Keys";
        public const string Audio = "WhatsApp Audio Keys";
        public const string Document = "WhatsApp Document Keys";

        public static Dictionary<string, string> Map = new Dictionary<string, string>{
            { Image,"/mms/image" },
            { Video,"/mms/video" },
            { Document,"/mms/document" },
            { Audio,"/mms/audio" },
        };
    }
}

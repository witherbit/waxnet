using System;
using System.Collections.Generic;
using System.Text;

namespace WAX.APIs
{
    public class InfoApi
    {
        private Api _api;
        public string UserId { get; internal set; }

        public string ChatId
        {
            get
            {
                return UserId.Replace("c.us", "s.whatsapp.net");
            }
        }

        public string PushName { get; internal set; }

        public int Battery { get; internal set; }

        public bool Plugged { get; internal set; }

        public bool Connect { get; internal set; }

        public string Version { get; internal set; }

        public string DeviceManufacturer { get; internal set; }

        public string DeviceModel { get; internal set; }

        public string OSVersion { get; internal set; }

        public string Platform { get; internal set; }

        public InfoApi(Api api)
        {
            _api = api;
        }
    }
}

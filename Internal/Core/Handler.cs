using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WAX;
using WAX.Models;
using WAX.Models.Messages;
using waxnet.Internal.Models;
using waxnet.Internal.Proto;
using waxnet.Internal.Utils;

namespace waxnet.Internal.Core
{
    class Handler
    {
        internal Api _api;
        internal async void Controller(ReceiveModel rm)
        {
            _api.CheckLicense();
            if (rm == null) return;
            if (rm.Body != null && rm.Body.Contains("Conn"))
            {
                ConnectInfo(rm);
                return;
            }
            var node = await _api.Engine.GetDecryptNode(rm);
            if(node != null && node.Content is List<Node> nodeList)
            {
                foreach (var item in nodeList)
                {
                    if (item.Description == "message")
                    {
                        Message(item);
                    }
                }
            }
        }

        internal async Task Message(Node node)
        {
            await Task.Run(()=>
            {
                var ms = WebMessageInfo.Parser.ParseFrom(node.Content as byte[]);
                if(ms.Message != null && ms.Key != null && ms.Key.RemoteJid != null)
                {
                    if (ms.Key.RemoteJid.Contains("@g.us"))
                    {
                        _api.CallEvent(_api, new CallEventArgs
                        {
                            Type = CallEventType.GroupMessage,
                            Content = GroupMessage.Build(ms, _api)
                        });
                    }
                    else
                    {
                        _api.CallEvent(_api, new CallEventArgs
                        {
                            Type = CallEventType.Message,
                            Content = ChatMessage.Build(ms, _api)
                        });
                    }
                }
            });
        }

        internal async Task ConnectInfo(ReceiveModel rm)
        {
            await Task.Run(() =>
            {
                var json = JArray.Parse(rm.Body)[1];
                _api.UserInfo = new UserInfo
                {
                    PushName = json["pushname"].ToString(),
                    Id = _api.Engine.SessionManager.Session.Id.GetId(),
                    Version = json["phone"]["wa_version"].ToString()
                };
                _api.DeviceInfo = new DeviceInfo
                {
                    Battery = Convert.ToInt32(json["battery"]),
                    Plugged = Convert.ToBoolean(json["plugged"]),
                    Connect = Convert.ToBoolean(json["connected"]),
                    OsVersion = json["phone"]["os_version"].ToString(),
                    Manufacturer = json["phone"]["device_manufacturer"].ToString(),
                    Model = json["phone"]["device_model"].ToString(),
                    Platform = json["platform"].ToString()
                };
            });
        }
    }
}

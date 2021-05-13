using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WAX;
using WAX.Enum;
using waxnet.Internal.Core;
using waxnet.Internal.Models;

namespace WAX.Methods
{
    public sealed class Profile
    {
        internal Api _api;
        public async void SetStatus(string status)
        {
            await Task.Run(() =>
            {
                var tag = _api.Engine.Tag;
                var n = new Node()
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api.Engine.ToString() },
                },
                    Content = new List<Node> {
                    new Node
                    {
                        Description = "status",
                        Attributes = null,
                        Content = Encoding.UTF8.GetBytes(status)
                    }
                }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }

        public async void SetName(string name)
        {
            await Task.Run(() =>
            {
                var tag = _api.Engine.Tag;
                var n = new Node()
                {
                    Description = "action",
                    Attributes = new Dictionary<string, string> {
                    { "type", "set" },
                    { "epoch", _api.Engine._msgCount.ToString() },
                },
                    Content = new List<Node> {
                    new Node
                    {
                        Description = "name",
                        Attributes = null,
                        Content = Encoding.UTF8.GetBytes(name)
                    }
                }
                };
                _api.Engine.SendBinary(n, WriteBinaryType.Profile, tag);
            });
        }
    }
}

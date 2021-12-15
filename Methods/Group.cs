using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using WAX;
using WAX.Models;
using waxnet.Internal.Core;
using waxnet.Internal.Utils;

namespace WAX.Methods
{
    public sealed class Group
    {
        internal Api _api;
        public GroupMetaDataInfo GetGroupMetaData(long ownerId, long groupId)
        {
            if (!_api.CheckLock()) return null;
            var json = JToken.Parse(_api.Engine.ReceiveManager.WaitResult(_api.Engine.SendJson($"[\"query\",\"GroupMetadata\",\"{groupId.GetGroupId(ownerId)}\"]")).Body);
            return GroupMetaDataInfo.Build(json);
        }
    }
}

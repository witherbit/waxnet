using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using waxnet.Internal.Utils;

namespace WAX.Models
{
    public sealed class GroupMetaDataInfo
    {
        internal static GroupMetaDataInfo Build(JToken json)
        {
            var gmdi = new GroupMetaDataInfo();
            gmdi.GroupId = json["id"].ToString().GetGroupId();
            gmdi.OwnerId = json["owner"].ToString().GetId();
            gmdi.Title = json["subject"].ToString();
            gmdi.CreationDateTime = json["creation"].ToString().GetDateTime();
            gmdi.SubjectDateTime = json["subjectTime"].ToString().GetDateTime();
            gmdi.SubjectOwnerId = json["subjectOwner"].ToString().GetId();
            gmdi.Description = json["desc"].ToString();
            gmdi.DescriptionId = json["descId"].ToString();
            gmdi.DescriptionDateTime = json["descTime"].ToString().GetDateTime();
            gmdi.DescriptionOwnerId = json["descOwner"].ToString().GetId();
            var list = new List<GroupParticipantInfo>();
            foreach (var jsonParticipant in json["participants"])
            {
                list.Add(GroupParticipantInfo.Build(jsonParticipant));
            }
            gmdi.Participans = list;
            return gmdi;
        }
        public (long OwnerId, long ChatId) GroupId { get; internal set; }
        public long OwnerId { get; internal set; }
        public string Title { get; internal set; }
        public DateTime CreationDateTime { get; set; }
        public List<GroupParticipantInfo> Participans { get; internal set; }
        public DateTime SubjectDateTime { get; set; }
        public long SubjectOwnerId { get; internal set; }
        public string Description { get; internal set; }
        public string DescriptionId { get; internal set; }
        public DateTime DescriptionDateTime { get; set; }
        public long DescriptionOwnerId { get; internal set; }
    }
    public sealed class GroupParticipantInfo
    {
        internal static GroupParticipantInfo Build(JToken json)
        {
            return new GroupParticipantInfo
            {
                Id = json["id"].ToString().GetId(),
                IsAdmin = json["isAdmin"].ToObject<bool>(),
                IsSuperAdmin = json["isSuperAdmin"].ToObject<bool>()
            };
        }
        public long Id { get; internal set; }
        public bool IsAdmin { get; internal set; }
        public bool IsSuperAdmin { get; internal set; }
    }
}

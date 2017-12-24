using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;



   /* 
    Licensed under the Apache License, Version 2.0
    
    http://www.apache.org/licenses/LICENSE-2.0
    */
namespace Hohoema.NicoAlert
{
    [XmlRoot(ElementName = "service")]
    public class FollowService
    {
        [XmlElement(ElementName = "id")]
        public List<string> Id { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string _Id { get; set; }
    }

    [XmlRoot(ElementName = "services")]
    public class FollowServices
    {
        [XmlElement(ElementName = "service")]
        public List<FollowService> Service { get; set; }
    }

    [XmlRoot(ElementName = "getcommunitylist")]
    public class FollowInfo
    {
        [XmlElement(ElementName = "services")]
        public FollowServices Services { get; set; }
        [XmlAttribute(AttributeName = "status")]
        public string Status { get; set; }
        [XmlAttribute(AttributeName = "time")]
        public string Time { get; set; }


        public IList<string> GetFollowCommunities()
        {
            var liveServiceId = NiconicoAlertServiceType.Live.ToString().ToLower();
            return Services?.Service.FirstOrDefault(x => x._Id == liveServiceId).Id;
        }
        public IList<string> GetFollowUsersOrChannels()
        {
            var videoServiceId = NiconicoAlertServiceType.Video.ToString().ToLower();
            return Services?.Service.FirstOrDefault(x => x._Id == videoServiceId).Id;
        }
    }

}

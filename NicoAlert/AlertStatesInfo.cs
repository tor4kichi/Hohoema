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


// Note: http://alert.nicovideo.jp/front/getalertstatus


namespace Hohoema.NicoAlert
{
    [XmlRoot(ElementName = "ms")]
    public class Ms
    {
        [XmlElement(ElementName = "addr")]
        public string Addr { get; set; }
        [XmlElement(ElementName = "port")]
        public string Port { get; set; }
    }

    [XmlRoot(ElementName = "service")]
    public class Service
    {
        [XmlElement(ElementName = "id")]
        public string Id { get; set; }
        [XmlElement(ElementName = "thread")]
        public string Thread { get; set; }
    }

    [XmlRoot(ElementName = "services")]
    public class Services
    {
        [XmlElement(ElementName = "service")]
        public List<Service> Service { get; set; }
    }

    [XmlRoot(ElementName = "getalertstatus")]
    public class AlertStatesInfo
    {
        [XmlElement(ElementName = "user_id")]
        public string UserId { get; set; }
        [XmlElement(ElementName = "user_hash")]
        public string UserHash { get; set; }
        [XmlElement(ElementName = "user_name")]
        public string UserName { get; set; }
        [XmlElement(ElementName = "user_prefecture")]
        public string UserPrefecture { get; set; }
        [XmlElement(ElementName = "user_age")]
        public string UserAge { get; set; }
        [XmlElement(ElementName = "user_sex")]
        public string UserSex { get; set; }
        [XmlElement(ElementName = "is_premium")]
        public string IsPremium { get; set; }
        [XmlElement(ElementName = "ms")]
        public Ms Ms { get; set; }
        [XmlElement(ElementName = "services")]
        public Services Services { get; set; }
        [XmlAttribute(AttributeName = "status")]
        public string Status { get; set; }
        [XmlAttribute(AttributeName = "time")]
        public string Time { get; set; }
    }

}

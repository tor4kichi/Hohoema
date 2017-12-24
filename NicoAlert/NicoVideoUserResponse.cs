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

    [XmlRoot(ElementName = "nicovideo_user_response")]
    public class NicoVideoUserResponse
    {
        [XmlElement(ElementName = "ticket")]
        public string Ticket { get; set; }
        [XmlAttribute(AttributeName = "status")]
        public string Status { get; set; }
    }

}


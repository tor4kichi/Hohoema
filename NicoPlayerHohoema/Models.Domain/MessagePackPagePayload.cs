using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public class MessagePackPagePayload
    {
        [DataMember(Name ="data")]
        public Byte[] Data { get; set; }

        public T Deserialize<T>()
        {
            return MessagePack.MessagePackSerializer.Deserialize<T>(Data);
        }
    }
}

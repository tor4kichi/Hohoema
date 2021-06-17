using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.NMSG_Comment
{
    public class NMSG_Response
    {
        public NGMS_Thread_Response[] Threads { get; internal set; }

        public ThreadType ThreadType { get; internal set; }

        public NMSG_Chat[] Comments { get; internal set; }

        public ThreadLeaf[] Leaves { get; internal set; }

        public NGMS_GlobalNumRes GlobalNumRes { get; internal set; }
    }


    public class ThreadLeaf
    {
        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("leaf")]
        public long? Leaf { get; set; }

        [JsonPropertyName("count")]
        public long Count { get; set; }
    }

    public class NGMS_Thread_ResponseItem
    {
        [JsonPropertyName("thread")]
        public NGMS_Thread_Response Thread { get; set; }
    }

    public class NGMS_Thread_Response
    {
        [JsonPropertyName("resultcode")]
        public int Resultcode { get; set; }

        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("server_time")]
        public int ServerTime { get; set; }

        [JsonPropertyName("last_res")]
        public int LastRes { get; set; }

        [JsonPropertyName("ticket")]
        public string Ticket { get; set; }

        [JsonPropertyName("revision")]
        public int Revision { get; set; }
    }

    public class NGMS_Leaf
    {

        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("leaf")]
        public int? Leaf { get; set; }
    }

    public class NGMS_GlobalNumRes
    {

        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("num_res")]
        public int NumRes { get; set; }
    }

    public class NMSG_Chat
    {

        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("no")]
        public uint No { get; set; }

        [JsonPropertyName("vpos")]
        public int Vpos { get; set; }

        [JsonPropertyName("leaf")]
        public int? Leaf { get; set; }

        [JsonPropertyName("date")]
        public int Date { get; set; }

        [JsonPropertyName("date_usec")]
        public int DateUsec { get; set; }

        [JsonPropertyName("premium")]
        public int? Premium { get; set; }

        [JsonPropertyName("anonymity")]
        public int? Anonymity { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("deleted")]
        public int? Deleted { get; set; }

        [JsonPropertyName("yourpost")]
        public int? Yourpost { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.NMSG_Comment
{
    internal interface ICommentSessionCommand { }

    internal interface ICommentSessionCommand_Sending : ICommentSessionCommand { }

    internal interface ICommentSessionCommand_Recieving : ICommentSessionCommand { }

    public class PingItem : ICommentSessionCommand
    {
        public PingItem()
        {

        }

        public PingItem(string content)
        {
            Ping.Content = content;
        }


        [JsonPropertyName("ping")]
        public Ping Ping { get; set; } = new Ping();
    }


    public class Ping 
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ThreadItem : ICommentSessionCommand_Sending
    {
        [JsonPropertyName("thread")]
        public Thread_CommentRequest Thread { get; set; }
    }

    public class Thread_CommentRequest
    {
        [JsonPropertyName("fork")]
        public int? Fork { get; set; } = null;

        [JsonPropertyName("thread")]
        public string ThreadId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "20090904"; // 投コメ取得の場合だけ "20061206"

        [JsonPropertyName("language")]
        public int? Language { get; set; } = 0;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("with_global")]
        public int? WithGlobal { get; set; } = 1;

        [JsonPropertyName("scores")]
        public int? Scores { get; set; } = 1;

        [JsonPropertyName("nicoru")]
        public int? Nicoru { get; set; } = 3;

        [JsonPropertyName("userkey")]
        public string Userkey { get; set; }

        [JsonPropertyName("force_184")]
        public string Force184 { get; set; } = null; // 公式動画のみ"1"

        [JsonPropertyName("threadkey")]
        public string Threadkey { get; set; } = null; // 公式動画のみ必要

        [JsonPropertyName("res_from")]
        public int? ResFrom { get; set; } = null;

    }

    public class ThreadLeavesItem : ICommentSessionCommand_Sending
    {
        [JsonPropertyName("thread_leaves")]
        public ThreadLeaves ThreadLeaves { get; set; }
    }

    public class ThreadLeaves
    {
        [JsonPropertyName("fork")]
        public int Fork { get; set; }

        [JsonPropertyName("thread")]
        public string ThreadId { get; set; }

        [JsonPropertyName("language")]
        public int Language { get; set; } = 0;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("scores")]
        public int Scores { get; set; } = 1;

        [JsonPropertyName("nicoru")]
        public int Nicoru { get; set; } = 3;

        [JsonPropertyName("userkey")]
        public string Userkey { get; set; }

        [JsonPropertyName("force_184")]
        public string Force184 { get; set; } = null; // 公式動画のみ"1"

        [JsonPropertyName("threadkey")]
        public string Threadkey { get; set; } = null; // 公式動画のみ必要



        public static string MakeContentString(TimeSpan videoLength)
        {
            return $"0-{(int)(Math.Ceiling(videoLength.TotalMinutes))}:100,1000";
        }
    }

    public sealed class PostChatData : ICommentSessionCommand_Sending
    {
        [JsonPropertyName("chat")]
        public PostChat Chat { get; set; }
    }

    public sealed class PostChat
    {
        [JsonPropertyName("thread")]
        public string ThreadId { get; set; }

        [JsonPropertyName("vpos")]
        public int Vpos { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("ticket")]
        public string Ticket { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("postkey")]
        public string PostKey { get; set; }

//        [JsonPropertyName("premium")]
//        public string Premium { get; set; } = "0"; // 一般ユーザー:0 プレミアム会員:1

    }
}

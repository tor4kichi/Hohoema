using System;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    public struct CommentPostResult
    {
        public int StatusCode { get; set; }
        public int CommentNo { get; set; }
        public TimeSpan VideoPosition { get; set; }
        public string ThreadId { get; set; }

        public bool IsSuccessed => Status == ChatResult.Success;
        public ChatResult Status => (ChatResult)StatusCode;
    }
}

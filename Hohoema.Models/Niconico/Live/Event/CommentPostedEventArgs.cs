using Hohoema.Models.Repository.Niconico.NicoVideo.Comment;

namespace Hohoema.Models.Live
{
    public struct CommentPostedEventArgs
    {
        public string Thread { get; set; }
        public ChatResult ChatResult { get; set; }
        public int No { get; set; }
    }
}

using Mntone.Nico2.Videos.Comment;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    public class CommentSubmitInfo
    {
        public string Ticket { get; set; }
        public int CommentCount { get; set; }

        public ThreadType ThreadType { get; set; }
    }
}

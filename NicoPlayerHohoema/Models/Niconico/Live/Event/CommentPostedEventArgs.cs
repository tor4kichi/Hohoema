using Mntone.Nico2.Videos.Comment;

namespace NicoPlayerHohoema.Models.Live
{
    public struct CommentPostedEventArgs
    {
        public string Thread { get; set; }
        public ChatResult ChatResult { get; set; }
        public int No { get; set; }
    }
}

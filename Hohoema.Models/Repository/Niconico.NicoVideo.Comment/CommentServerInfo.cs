namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    public class CommentServerInfo
    {
        public int ViewerUserId { get; set; }
        public string VideoId { get; set; }
        public string ServerUrl { get; set; }
        public int DefaultThreadId { get; set; }
        public int? CommunityThreadId { get; set; }

        public bool ThreadKeyRequired { get; set; }
    }
}

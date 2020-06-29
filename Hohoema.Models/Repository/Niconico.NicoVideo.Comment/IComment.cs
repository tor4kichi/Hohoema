
namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    public interface IComment
    {
        uint CommentId { get; set; }
        string CommentText { get; set; }
        bool IsDeleted { get; }
        string Mail { get; set; }
        int NGScore { get; set; }
        string UserId { get; set; }
        long VideoPosition { get; set; }
    }
}
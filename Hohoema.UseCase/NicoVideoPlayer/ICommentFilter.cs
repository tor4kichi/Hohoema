using Hohoema.Models.Repository.Niconico.NicoVideo.Comment;

namespace Hohoema.UseCase.NicoVideoPlayer
{
    public interface ICommentFilter
    {
        bool IsHiddenComment(IComment comment);
        string TransformCommentText(string CommentText);
        bool IsIgnoreCommand(string command);
    }
}

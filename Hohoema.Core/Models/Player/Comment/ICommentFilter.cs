namespace Hohoema.Models.Player.Comment;

public interface ICommentFilter
{
    bool IsHiddenComment(IComment comment);
    string TransformCommentText(string CommentText);
    bool IsIgnoreCommand(string command);
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Player.Comment;

public struct CommentPostResult
{
    public int CommentNo { get; set; }
    public TimeSpan VideoPosition { get; set; }
    public string ThreadId { get; set; }
    public string ThreadForkLabel { get; set; }
    public bool IsSuccessed { get; set; }
}

public interface ICommentSession<TComment> : IDisposable where TComment : IComment
{
    string ContentId { get; }
    string UserId { get; }

    event EventHandler<TComment> RecieveComment;

    Task<IEnumerable<TComment>> GetInitialComments();

    bool CanPostComment { get; }

    Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands);
}

/*
public class LiveCommentService : ICommentSession
{
    public LiveCommentService()
    {

    }

    public event EventHandler<Comment> RecieveComment;

    void IDisposable.Dispose()
    {

    }

    public async Task<List<Comment>> GetInitialComments()
    {
        throw new NotImplementedException();
    }
}

public class TimeshiftCommentService : ICommentSession
{
    public TimeshiftCommentService()
    {

    }

    public event EventHandler<Comment> RecieveComment;


    void IDisposable.Dispose()
    {

    }

    public async Task<List<Comment>> GetInitialComments()
    {
        throw new NotImplementedException();
    }
}
*/

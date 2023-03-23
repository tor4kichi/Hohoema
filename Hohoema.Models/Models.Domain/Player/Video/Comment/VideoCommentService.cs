using Hohoema.Models.Domain.Player.Comment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Player.Video.Comment;

public class VideoCommentService : ICommentSession<VideoComment>
{
    CommentClient CommentClient;

    public string ContentId => CommentClient.RawVideoId;
    public string UserId { get; }

    public VideoCommentService(CommentClient commentClient, string userId)
    {
        CommentClient = commentClient;
        UserId = userId;
    }


    public event EventHandler<VideoComment> RecieveComment;

    void IDisposable.Dispose()
    {

    }

    public async Task<IEnumerable<VideoComment>> GetInitialComments()
    {
        return await CommentClient.GetCommentsAsync();
    }

    public bool CanPostComment => CommentClient.CanSubmitComment;
    public async Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands)
    {
        return await CommentClient.SubmitComment(message, position, commands);
    }
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

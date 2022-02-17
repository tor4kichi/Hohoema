using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Video.Watch.NMSG_Comment;

namespace Hohoema.Models.Domain.Player.Comment
{
    public struct CommentPostResult
    {
        public ChatResultCode StatusCode { get; set; }
        public int CommentNo { get; set; }
        public TimeSpan VideoPosition { get; set; }
        public string ThreadId { get; set; }

        public bool IsSuccessed => Status == ChatResultCode.Success;
        public ChatResultCode Status => StatusCode;
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
}

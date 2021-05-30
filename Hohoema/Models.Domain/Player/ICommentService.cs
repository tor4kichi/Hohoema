using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Video.Watch.NMSG_Comment;

namespace Hohoema.Models.Domain.Player
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

    public interface ICommentSession : IDisposable
    {
        string ContentId { get; }
        string UserId { get; }

        event EventHandler<IComment> RecieveComment;

        Task<IReadOnlyCollection<IComment>> GetInitialComments();

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

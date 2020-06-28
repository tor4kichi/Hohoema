using Mntone.Nico2.Videos.Comment;
using Hohoema.Models;
using Hohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico
{
    public struct CommentPostResult
    {
        public int StatusCode { get; set; }
        public int CommentNo { get; set; }
        public TimeSpan VideoPosition { get; set; }
        public string ThreadId { get; set; }

        public bool IsSuccessed => Status == ChatResult.Success;
        public ChatResult Status => (ChatResult)StatusCode;
    }

    public interface ICommentSession : IDisposable
    {
        string ContentId { get; }
        string UserId { get; }

        event EventHandler<Comment> RecieveComment;

        Task<IReadOnlyCollection<Comment>> GetInitialComments();

        bool CanPostComment { get; }

        Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands);
    }

    public class VideoCommentService : ICommentSession
    {
        CommentClient CommentClient;

        public string ContentId => CommentClient.RawVideoId;
        public string UserId => CommentClient.NiconicoSession.UserIdString;

        public VideoCommentService(CommentClient commentClient)
        {
            CommentClient = commentClient;
        }


        public event EventHandler<Comment> RecieveComment;

        void IDisposable.Dispose()
        {

        }

        public async Task<IReadOnlyCollection<Comment>> GetInitialComments()
        {
            return await CommentClient.GetCommentsAsync();
        }

        public bool CanPostComment => CommentClient.CanSubmitComment;
        public async Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands)
        {
            var res = await CommentClient.SubmitComment(message, position, commands);

            var chatResult = res.Chat_result;
            return new CommentPostResult()
            {
                CommentNo = chatResult.No,
                StatusCode = chatResult.__Status,
                ThreadId = chatResult.Thread,
                VideoPosition = position,
            };

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
}

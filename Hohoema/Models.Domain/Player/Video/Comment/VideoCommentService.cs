using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Player.Video.Comment
{
    public class VideoCommentService : ICommentSession
    {
        CommentClient CommentClient;

        public string ContentId => CommentClient.RawVideoId;
        public string UserId => CommentClient.NiconicoSession.UserIdString;

        public VideoCommentService(CommentClient commentClient)
        {
            CommentClient = commentClient;
        }


        public event EventHandler<IComment> RecieveComment;

        void IDisposable.Dispose()
        {

        }

        public async Task<IReadOnlyCollection<IComment>> GetInitialComments()
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

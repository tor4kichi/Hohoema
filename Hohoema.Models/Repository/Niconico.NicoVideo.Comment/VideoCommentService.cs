using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hohoema.Models.Repository.Niconico.NicoVideo;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    using NiconicoSession = Models.Niconico.NiconicoSession;

    public class VideoCommentService : ICommentSession
    {
        CommentClient CommentClient;

        public string ContentId => CommentClient.RawVideoId;
        public string UserId => CommentClient.UserId;

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

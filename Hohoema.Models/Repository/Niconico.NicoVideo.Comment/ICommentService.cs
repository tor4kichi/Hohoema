using Mntone.Nico2.Videos.Comment;
using Hohoema.Models;
using Hohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Repository.Niconico.NicoVideo;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{

    public interface ICommentSession : IDisposable
    {
        string ContentId { get; }
        string UserId { get; }

        event EventHandler<Comment> RecieveComment;

        Task<IReadOnlyCollection<Comment>> GetInitialComments();

        bool CanPostComment { get; }

        Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands);
    }

}

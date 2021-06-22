using Hohoema.Models.Domain.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Player.Comment
{
    public interface ICommentFilter
    {
        bool IsHiddenComment(IComment comment);
        string TransformCommentText(string CommentText);
        bool IsIgnoreCommand(string command);
    }
}

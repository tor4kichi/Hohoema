using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Models.Niconico
{
    public interface IComment
    {
        Color? Color { get; set; }
        uint CommentId { get; set; }
        string CommentText { get; set; }
        string CommentText_Transformed { get; set; }
        int DeletedFlag { get; set; }
        CommentDisplayMode DisplayMode { get; set; }
        bool IsAnonymity { get; set; }
        bool IsInvisible { get; set; }
        bool IsLoginUserComment { get; set; }
        bool IsOwnerComment { get; set; }
        bool IsScrolling { get; }
        string Mail { get; set; }
        CommentSizeMode SizeMode { get; set; }
        string UserId { get; set; }
        TimeSpan VideoPosition { get; set; }
    }

}

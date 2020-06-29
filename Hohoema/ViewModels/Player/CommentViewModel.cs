using Hohoema.Models.Repository.Niconico.NicoVideo.Comment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Hohoema.ViewModels.Player
{
    public enum CommentDisplayMode
    {
        Scrolling,
        Top,
        Center,
        Bottom,
    }

    public enum CommentSizeMode
    {
        Normal,
        Big,
        Small,
    }

    public class CommentViewModel : IComment
    {
        public uint CommentId { get; set; }

        public string CommentText { get; set; }

        public string Mail { get; set; }
        public string UserId { get; set; }

        public bool IsAnonimity { get; set; }

        public long VideoPosition { get; set; }

        public int NGScore { get; set; }

        public bool IsDeleted => DeletedFlag != 0;

        public int DeletedFlag { get; set; }


        public bool IsInvisible { get; set; }
        private string _commentText_Transformed;
        private Comment _comment;

        public CommentViewModel(Comment comment)
        {
            _comment = comment;
        }

        public string CommentText_Transformed
        {
            get => _commentText_Transformed ?? CommentText;
            set => _commentText_Transformed = value;
        }


        public CommentDisplayMode DisplayMode { get; set; }

        public bool IsScrolling => DisplayMode == CommentDisplayMode.Scrolling;


        public CommentSizeMode SizeMode { get; set; }


        public bool IsLoginUserComment { get; set; }

        public bool IsOwnerComment { get; set; }

        public Color? Color { get; set; }
    }
}

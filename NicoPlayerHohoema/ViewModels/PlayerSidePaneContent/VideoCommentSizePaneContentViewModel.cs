using NicoPlayerHohoema.UseCase;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public sealed class VideoCommentSizePaneContentViewModel : BindableBase
    {
        public VideoCommentSizePaneContentViewModel(
            UseCase.CommentPlayer commentPlayer
            )
        {
            CommentPlayer = commentPlayer;
        }

        public CommentPlayer CommentPlayer { get; }
    }
}

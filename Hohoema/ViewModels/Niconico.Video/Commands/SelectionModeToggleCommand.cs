using Hohoema.Models.Niconico.Video;
using Hohoema.Models.UseCase.Playlist;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Niconico.Video;

namespace Hohoema.ViewModels.Niconico.Video.Commands
{ 
    public class SelectionModeToggleCommand : CommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var selectionContext = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoItemsSelectionContext>();
            if (selectionContext.IsSelectionEnabled)
            {
                selectionContext.EndSelectioin();
            }
            else
            {
                selectionContext.StartSelection();
            }
        }
    }
}

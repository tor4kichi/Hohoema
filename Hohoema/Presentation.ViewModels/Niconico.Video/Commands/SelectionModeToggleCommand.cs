using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideos;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{ 
    public class SelectionModeToggleCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var selectionContext = App.Current.Container.Resolve<VideoItemsSelectionContext>();
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

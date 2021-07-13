using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Hohoema.Models.UseCase.Niconico.Video;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class SelectionExitCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var selectionContext = App.Current.Container.Resolve<VideoItemsSelectionContext>();
            selectionContext.EndSelectioin();
        }
    }
}

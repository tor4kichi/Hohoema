using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
namespace Hohoema.Models.UseCase.Playlist.Commands
{
    public sealed class SelectionAllSelectCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var selectionContext = App.Current.Container.Resolve<UseCase.Playlist.VideoItemsSelectionContext>();
            selectionContext.ToggleSelectAll();
        }
    }
}

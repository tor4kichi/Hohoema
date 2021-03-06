﻿using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.Niconico.Video;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class SelectionStartCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var selectionContext = App.Current.Container.Resolve<VideoItemsSelectionContext>();
            selectionContext.StartSelection(parameter as IVideoContent);
        }
    }
}

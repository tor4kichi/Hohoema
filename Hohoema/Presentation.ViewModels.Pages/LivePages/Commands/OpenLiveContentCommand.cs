using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Presentation.Services.Player;
using Microsoft.Toolkit.Mvvm.Messaging;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.LivePages.Commands
{
    public sealed class OpenLiveContentCommand : DelegateCommandBase
    {
        public OpenLiveContentCommand()
        {
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is ILiveContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ILiveContent liveContent)
            {
                StrongReferenceMessenger.Default.Send(new PlayerPlayLiveRequestMessage(new () { LiveId = liveContent.Id }));
            }
        }
    }
}

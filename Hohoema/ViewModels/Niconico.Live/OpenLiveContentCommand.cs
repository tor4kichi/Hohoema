using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.UseCase.Niconico.Player.Events;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Live
{
    public sealed class OpenLiveContentCommand : CommandBase
    {
        private readonly IMessenger _messenger;

        public OpenLiveContentCommand(IMessenger messenger)
        {
            _messenger = messenger;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is ILiveContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ILiveContent liveContent)
            {
                _messenger.Send(new PlayerPlayLiveRequestMessage(new () { LiveId = liveContent.LiveId }));
            }
        }
    }
}

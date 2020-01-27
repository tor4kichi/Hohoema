using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands
{
    public sealed class OpenLiveContentCommand : DelegateCommandBase
    {
        private readonly IEventAggregator _eventAggregator;

        public OpenLiveContentCommand(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.ILiveContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.ILiveContent liveContent)
            {
                _eventAggregator.GetEvent<Services.Player.PlayerPlayLiveRequest>()
                    .Publish(new Services.Player.PlayerPlayLiveRequestEventArgs() { LiveId = liveContent.Id });
            }
        }
    }
}

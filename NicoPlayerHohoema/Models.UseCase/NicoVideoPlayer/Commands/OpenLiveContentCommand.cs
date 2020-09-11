using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Presentation.Services.Player;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.NicoVideoPlayer.Commands
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
            return parameter is ILiveContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ILiveContent liveContent)
            {
                _eventAggregator.GetEvent<PlayerPlayLiveRequest>()
                    .Publish(new PlayerPlayLiveRequestEventArgs() { LiveId = liveContent.Id });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class AddAfterViewPlaylistCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var player = HohoemaCommnadHelper.GetHohoemaPlaylist();
                player.DefaultPlaylist.AddVideo(content.Id, content.Label);
            }
        }
    }
}

using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Services;
using I18NPortable;
using Hohoema.Infra;

namespace Hohoema.ViewModels.Player.Video
{
    public sealed class ChangeVideoQualityCommand : CommandBase
    {
        private readonly HohoemaPlaylistPlayer _playlistPlayer;
        private readonly INotificationService _notificationService;

        public ChangeVideoQualityCommand(HohoemaPlaylistPlayer playlistPlayer, INotificationService notificationService)
        {
            _playlistPlayer = playlistPlayer;
            _notificationService = notificationService;
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is NicoVideoQuality) { return true; }
            else if (parameter is NicoVideoQualityEntity entity) { return entity?.IsAvailable ?? false; }
            else { return false; }
        }

        protected override async void Execute(object parameter)
        {
            try
            {

                if (parameter is NicoVideoQuality quality)
                {
                    if (!_playlistPlayer.CanPlayQuality(quality)) { return; }
                    await _playlistPlayer.ChangeQualityAsync(quality);

                    _notificationService.ShowLiteInAppNotification_Success("Notification_VideoQualityChanged_Success".Translate(quality.Translate()));
                }
                else if (parameter is NicoVideoQualityEntity qualityEntity)
                {
                    if (!_playlistPlayer.CanPlayQuality(qualityEntity.Quality)) { return; }
                    await _playlistPlayer.ChangeQualityAsync(qualityEntity.Quality);

                    _notificationService.ShowLiteInAppNotification_Success("Notification_VideoQualityChanged_Success".Translate(qualityEntity.Quality.Translate()));
                }
                else
                {
                    throw new HohoemaException("画質変更に失敗");
                }
            }
            catch
            {
                _notificationService.ShowLiteInAppNotification_Fail("Notification_VideoQualityChanged_Failed".Translate());
            }
        }
    }
}

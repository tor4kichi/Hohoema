using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.NicoVideos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    /// <summary>
    /// サムネイルが設定されていないローカルマイリストに対して先頭動画を参照してサムネイルURLを埋める
    /// </summary>
    public class LocalMylistThumbnailImageMigration_V_0_28_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly PlaylistRepository _playlistRepository;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public LocalMylistThumbnailImageMigration_V_0_28_0(
            AppFlagsRepository appFlagsRepository,
            PlaylistRepository playlistRepository,
            NicoVideoCacheRepository nicoVideoRepository,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _playlistRepository = playlistRepository;
            _nicoVideoRepository = nicoVideoRepository;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public async Task MigrateAsync()
        {
            if (_appFlagsRepository.IsLocalMylistThumbnailImageMigration_V_0_28_0 is true) { return; }

            _appFlagsRepository.IsLocalMylistThumbnailImageMigration_V_0_28_0 = true;

            foreach (var entity in _playlistRepository.GetAllPlaylist())
            {
                if (entity.ThumbnailImage == null && _playlistRepository.GetCount(entity.Id) != 0)
                {
                    var firstItem = _playlistRepository.GetItems(entity.Id, 0, 1).First();
                    var firstVideo = await _nicoVideoProvider.GetNicoVideoInfo(firstItem.ContentId);
                    entity.ThumbnailImage = new Uri(firstVideo.ThumbnailUrl);

                    _playlistRepository.UpsertPlaylist(entity);

                    Debug.WriteLine($"ローカルマイリスト {entity.Label} のサムネを追加");
                }
            }
        }
    }
}

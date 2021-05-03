using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Presentation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    internal sealed class VideoCacheDatabaseMigration_V_0_29_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly CacheRequestRepository _cacheRequestRepositoryLegacy;
        private readonly VideoCacheSaveFolderManager _videoCacheSaveFolderManager;
        private readonly VideoCacheManager _videoCacheManager;
        private readonly DialogService _dialogService;

        public VideoCacheDatabaseMigration_V_0_29_0(
            Domain.Application.AppFlagsRepository appFlagsRepository,
            Domain.Player.Video.Cache.CacheRequestRepository cacheRequestRepositoryLegacy,
            Domain.VideoCache.VideoCacheSaveFolderManager videoCacheSaveFolderManager,
            Domain.VideoCache.VideoCacheManager videoCacheManager,
            Presentation.Services.DialogService dialogService
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _cacheRequestRepositoryLegacy = cacheRequestRepositoryLegacy;
            _videoCacheSaveFolderManager = videoCacheSaveFolderManager;
            _videoCacheManager = videoCacheManager;
            _dialogService = dialogService;
        }

        public async Task MigrateAsync()
        {
            using var _ = _appFlagsRepository.GetCacheVideoMigration();

            var saveFolder = await _videoCacheSaveFolderManager.GetVideoCacheFolder();

            // DB的な統合をやる
            // ファイルの暗号化は別口でダイアログ表示と一緒に実行する
            foreach (var regacyItem in _cacheRequestRepositoryLegacy.GetRange(0, int.MaxValue))
            {
                var query = saveFolder.CreateFileQuery();
                query.ApplyNewQueryOptions(new Windows.Storage.Search.QueryOptions() { UserSearchFilter = regacyItem.VideoId });
                var file = (await query.GetFilesAsync(0, 1)).FirstOrDefault();

                NicoVideoCacheQuality newQuality;
                if (file != null)
                {
                    try
                    {
                        (var _, var regacyQuality) = VideoCacheManagerLegacy.CacheRequestInfoFromFileName(file);
                        newQuality = ToNewQuality(regacyQuality);
                    }
                    catch
                    {
                        newQuality = NicoVideoCacheQuality.Unknown;
                    }
                }
                else
                {
                    newQuality = ToNewQuality(regacyItem.PriorityQuality);
                }

                await _videoCacheManager.PushCacheRequestAsync(regacyItem.VideoId, newQuality);
            }
        }


        private static NicoVideoCacheQuality ToNewQuality(Domain.NicoVideoQuality regacyQuality)
        {
            return regacyQuality switch
            {
                Domain.NicoVideoQuality.SuperHigh => NicoVideoCacheQuality.SuperHigh,
                Domain.NicoVideoQuality.High => NicoVideoCacheQuality.High,
                Domain.NicoVideoQuality.Midium => NicoVideoCacheQuality.Midium,
                Domain.NicoVideoQuality.Low => NicoVideoCacheQuality.Low,
                Domain.NicoVideoQuality.Mobile => NicoVideoCacheQuality.SuperLow,
                _ => NicoVideoCacheQuality.Unknown,

            };
        }
    }
}

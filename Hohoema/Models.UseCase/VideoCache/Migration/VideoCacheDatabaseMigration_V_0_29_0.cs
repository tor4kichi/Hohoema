using Hohoema.Models.Domain;
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
using Windows.Storage.AccessCache;

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
            using var releaser = _appFlagsRepository.GetCacheVideoMigration();

            var saveFolder = await _videoCacheSaveFolderManager.GetVideoCacheFolder();

            // 保存先フォルダを移行
            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(VideoCacheSaveFolderManager.FolderAccessToken))
            {
                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(VideoCacheSaveFolderManager.FolderAccessToken);
                StorageApplicationPermissions.FutureAccessList.Remove(VideoCacheSaveFolderManager.FolderAccessToken);

                StorageApplicationPermissions.FutureAccessList.AddOrReplace(VideoCache.VideoCacheFolderManager.CACHE_FOLDER_NAME, folder);
            }


            // DB的な統合をやる
            foreach (var regacyItem in _cacheRequestRepositoryLegacy.GetRange(0, int.MaxValue))
            {
                var query = saveFolder.CreateFileQuery();
                query.ApplyNewQueryOptions(new Windows.Storage.Search.QueryOptions() { UserSearchFilter = regacyItem.VideoId });
                var file = (await query.GetFilesAsync(0, 1)).FirstOrDefault();

                NicoVideoQuality newQuality;
                if (file != null)
                {
                    try
                    {
                        (_, newQuality) = VideoCacheManagerLegacy.CacheRequestInfoFromFileName(file);
                    }
                    catch
                    {
                        newQuality = NicoVideoQuality.Unknown;
                    }
                }
                else
                {
                    newQuality = NicoVideoQuality.Unknown;
                }

                _videoCacheManager.PushCacheRequest_Legacy(regacyItem.VideoId, newQuality);
            }
        }
    }
}

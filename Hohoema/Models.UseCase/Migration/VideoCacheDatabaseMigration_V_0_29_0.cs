using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.VideoCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    internal sealed class VideoCacheDatabaseMigration_V_0_29_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly CacheRequestRepository _cacheRequestRepositoryLegacy;
        private readonly VideoCacheSaveFolderManager _videoCacheSaveFolderManager;
        private readonly VideoCacheManager _videoCacheManager;

        public VideoCacheDatabaseMigration_V_0_29_0(
            Domain.Application.AppFlagsRepository appFlagsRepository,
            Domain.Player.Video.Cache.CacheRequestRepository cacheRequestRepositoryLegacy,
            Domain.VideoCache.VideoCacheSaveFolderManager videoCacheSaveFolderManager,
            Domain.VideoCache.VideoCacheManager videoCacheManager
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _cacheRequestRepositoryLegacy = cacheRequestRepositoryLegacy;
            _videoCacheSaveFolderManager = videoCacheSaveFolderManager;
            _videoCacheManager = videoCacheManager;
        }

        public async Task MigrateAsync()
        {
            // TODO: ダイアログで進捗表示と入力阻害

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

                await _videoCacheManager.PushCacheRequestAsync_Legacy(regacyItem.VideoId, newQuality, file);
            }

            // TODO: ダイアログを表示してキャッシュ暗号化を処理する
            // _videoCacheManager.PrepareNextEncryptLegacyVideoTaskAsync();

        }

        private static NicoVideoCacheQuality ToNewQuality(Domain.NicoVideoQuality regacyQuality)
        {
            return regacyQuality switch
            {
                Domain.NicoVideoQuality.Unknown => NicoVideoCacheQuality.Unknown,
                Domain.NicoVideoQuality.Smile_Original => NicoVideoCacheQuality.Unknown,
                Domain.NicoVideoQuality.Smile_Low => NicoVideoCacheQuality.Unknown,
                Domain.NicoVideoQuality.Dmc_SuperHigh => NicoVideoCacheQuality.SuperHigh,
                Domain.NicoVideoQuality.Dmc_High => NicoVideoCacheQuality.Midium,
                Domain.NicoVideoQuality.Dmc_Midium => NicoVideoCacheQuality.Midium,
                Domain.NicoVideoQuality.Dmc_Low => NicoVideoCacheQuality.Low,
                Domain.NicoVideoQuality.Dmc_Mobile => NicoVideoCacheQuality.SuperLow,
            };
        }
    }
}

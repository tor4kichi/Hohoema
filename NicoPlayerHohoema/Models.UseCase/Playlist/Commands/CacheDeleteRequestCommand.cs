using I18NPortable;

using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Presentation.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.Commands
{
    public sealed class CacheDeleteRequestCommand : VideoContentSelectionCommandBase
    {
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        public CacheDeleteRequestCommand(
            VideoCacheManager videoCacheManager,
            DialogService dialogService
            )
        {
            VideoCacheManager = videoCacheManager;
            DialogService = dialogService;
        }

        protected override async void Execute(IVideoContent content)
        {
            var cacheRequests = await VideoCacheManager.GetCachedAsync(content.Id);
            if (cacheRequests.Any())
            {
                var confirmed = await DialogService.ShowMessageDialog(
                    "ConfirmCacheRemoveContent".Translate(content.Label),
                    $"ConfirmCacheRemoveTitle".Translate(),
                    acceptButtonText: "Delete".Translate(),
                    "Cancel".Translate()
                    );
                if (confirmed)
                {
                    await VideoCacheManager.CancelCacheRequest(content.Id);
                }
            }
            else
            {
                await VideoCacheManager.CancelCacheRequest(content.Id);
            }
        }

        protected override async void Execute(IEnumerable<IVideoContent> items)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var anyCached = items.Any(x => VideoCacheManager.CheckCachedAsyncUnsafe(x.Id));
            if (anyCached)
            {
                var confirmed = await DialogService.ShowMessageDialog(
                    "ConfirmCacheRemoveContent_Multiple".Translate(),
                    $"ConfirmCacheRemoveTitle_Multiple".Translate(items.Count()),
                    acceptButtonText: "Delete".Translate(),
                    "Cancel".Translate()
                    );
                if (confirmed)
                {
                    foreach (var item in items)
                    {
                        await VideoCacheManager.CancelCacheRequest(item.Id);
                    }
                }
            }
            else
            {
                foreach (var item in items)
                {
                    await VideoCacheManager.CancelCacheRequest(item.Id);
                }
            }
        }


        public VideoCacheManager VideoCacheManager { get; }
        public DialogService DialogService { get; }
    }
}

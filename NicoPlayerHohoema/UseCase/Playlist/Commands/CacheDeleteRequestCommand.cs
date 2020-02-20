using I18NPortable;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Services.Helpers;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class CacheDeleteRequestCommand : VideoContentSelectionCommandBase
    {
        public CacheDeleteRequestCommand(
            Models.Cache.VideoCacheManager videoCacheManager,
            Services.DialogService dialogService
            )
        {
            VideoCacheManager = videoCacheManager;
            DialogService = dialogService;
        }

        protected override async void Execute(IVideoContent content)
        {
            var video = Database.NicoVideoDb.Get(content.Id);
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


        public Models.Cache.VideoCacheManager VideoCacheManager { get; }
        public Services.DialogService DialogService { get; }
    }
}

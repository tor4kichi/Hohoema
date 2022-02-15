using I18NPortable;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Domain.VideoCache;

namespace Hohoema.Presentation.ViewModels.VideoCache.Commands
{
    public sealed class CacheDeleteRequestCommand : VideoContentSelectionCommandBase
    {
        private readonly VideoCacheManager _videoCacheManager;
        private readonly DialogService _dialogService;

        public CacheDeleteRequestCommand(
            VideoCacheManager videoCacheManager,
            DialogService dialogService
            )
        {
            _videoCacheManager = videoCacheManager;
            _dialogService = dialogService;
        }

        protected override async void Execute(IVideoContent content)
        {
            var status = _videoCacheManager.GetVideoCacheStatus(content.VideoId);
            
            if (status is null) { return; }

            if (status is VideoCacheStatus.Completed)
            {
                var confirmed = await _dialogService.ShowMessageDialog(
                    "ConfirmCacheRemoveContent".Translate(content.Title),
                    $"ConfirmCacheRemoveTitle".Translate(),
                    acceptButtonText: "Delete".Translate(),
                    "Cancel".Translate()
                    );
                if (confirmed)
                {
                    await _videoCacheManager.CancelCacheRequestAsync(content.VideoId);
                }
            }
            else
            {
                await _videoCacheManager.CancelCacheRequestAsync(content.VideoId);
            }
        }

        protected override async void Execute(IEnumerable<IVideoContent> items)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
           // Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var anyCached = items.Any(x => _videoCacheManager.GetVideoCacheStatus(x.VideoId) is VideoCacheStatus.Completed);
            if (anyCached)
            {
                var confirmed = await _dialogService.ShowMessageDialog(
                    "ConfirmCacheRemoveContent_Multiple".Translate(),
                    $"ConfirmCacheRemoveTitle_Multiple".Translate(items.Count()),
                    acceptButtonText: "Delete".Translate(),
                    "Cancel".Translate()
                    );
                if (confirmed)
                {
                    foreach (var item in items)
                    {
                        await _videoCacheManager.CancelCacheRequestAsync(item.VideoId);
                    }
                }
            }
            else
            {
                foreach (var item in items)
                {
                    await _videoCacheManager.CancelCacheRequestAsync(item.VideoId);
                }
            }
        }
    }
}

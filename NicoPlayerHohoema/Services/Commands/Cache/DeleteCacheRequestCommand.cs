using NicoPlayerHohoema.Services.Helpers;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands.Cache
{
    public sealed class DeleteCacheRequestCommand : DelegateCommandBase
    {
        public DeleteCacheRequestCommand(
            Models.Cache.VideoCacheManager videoCacheManager,
            Services.DialogService dialogService
            )
        {
            VideoCacheManager = videoCacheManager;
            DialogService = dialogService;
        }


        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent content)
            {
                var video = Database.NicoVideoDb.Get(content.Id);
                var cacheRequests = await VideoCacheManager.GetCacheRequest(content.Id);
                var choiceItems = await DialogService.ShowMultiChoiceDialogAsync(
                    $"削除するキャッシュ動画を選択する\n「{video.Title}」",
                    cacheRequests,
                    Enumerable.Empty<Models.Cache.NicoVideoCacheRequest>(),
                    nameof(Models.Cache.NicoVideoCacheRequest.Quality)
                    );

                if (choiceItems?.Any() ?? false)
                {
                    foreach (var deleteItem in choiceItems)
                    {
                        await VideoCacheManager.DeleteCachedVideo(content.Id, deleteItem.Quality);
                    }
                }
            }
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent content)
            {
                return VideoCacheManager.CheckCached(content.Id);
            }
            else
            {
                return false;
            }
        }

        public Models.Cache.VideoCacheManager VideoCacheManager { get; }
        public Services.DialogService DialogService { get; }
    }
}

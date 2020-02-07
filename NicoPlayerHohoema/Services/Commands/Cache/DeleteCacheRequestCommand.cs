using I18NPortable;
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
                if (cacheRequests.Any())
                {
                    var choiceItems = await DialogService.ShowMultiChoiceDialogAsync(
                        $"SelectDeleteVideoQuality".Translate(),
                        cacheRequests,
                        Enumerable.Empty<Models.Cache.NicoVideoCacheRequest>(),
                        nameof(Models.Cache.NicoVideoCacheRequest.Quality)
                        );
                    if (choiceItems?.Any() ?? false)
                    {
                        foreach (var deleteItem in choiceItems)
                        {
                            await VideoCacheManager.CancelCacheRequest(content.Id, deleteItem.Quality);
                        }
                    }
                }
            }
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent content)
            {
                // Note: CheckCacheRequested はキャッシュ済みしか同期実行で返せない
                // これはVideoCacheManagerの設計が複雑で、
                // 「キャッシュ状態ごとの動画」を非同期に列挙する形になってることが原因
                // 本来は「動画ごとのキャッシュ状態」を非同期安全な形で同期的に取り出せる必要がある
                //return VideoCacheManager.CheckCacheRequested(content.Id);
                return true;
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

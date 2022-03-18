using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using NiconicoToolkit.Video;
using Microsoft.Toolkit.Mvvm.Input;
using Hohoema.Presentation.Navigations;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Niconico.Video;
using Microsoft.Extensions.Logging;
using ZLogger;
using Reactive.Bindings;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Activity
{
    public class WatchHistoryPageViewModel : HohoemaPageViewModelBase
	{
		public WatchHistoryPageViewModel(
            ILoggerFactory loggerFactory,
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            WatchHistoryManager watchHistoryManager,
            PageManager pageManager,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            WatchHistoryRemoveAllCommand watchHistoryRemoveAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
		{
            _logger = loggerFactory.CreateLogger<WatchHistoryPageViewModel>();
            ApplicationLayoutManager = applicationLayoutManager;
            _niconicoSession = niconicoSession;
            _watchHistoryManager = watchHistoryManager;
            PageManager = pageManager;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            WatchHistoryRemoveAllCommand = watchHistoryRemoveAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            Histories = new ObservableCollection<HistoryVideoListItemControlViewModel>();

            NowUpdating = new ReactivePropertySlim<bool>(false)
                .AddTo(_CompositeDisposable);
        }

        private readonly NiconicoSession _niconicoSession;
        private readonly WatchHistoryManager _watchHistoryManager;
        private readonly ILogger<WatchHistoryPageViewModel> _logger;

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public WatchHistoryRemoveAllCommand WatchHistoryRemoveAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public ObservableCollection<HistoryVideoListItemControlViewModel> Histories { get; }

        public ReactivePropertySlim<bool> NowUpdating { get; } 


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (RefreshCommand.CanExecute(null))
            {
                RefreshCommand.Execute(null);
            }


            Observable.FromEventPattern<WatchHistoryRemovedEventArgs>(
                h => _watchHistoryManager.WatchHistoryRemoved += h,
                h => _watchHistoryManager.WatchHistoryRemoved -= h
                )
                .Subscribe(e =>
                {
                    var args = e.EventArgs;
                    var removedItem = Histories.FirstOrDefault(x => x.VideoId == args.VideoId);
                    if (removedItem != null)
                    {
                        Histories.Remove(removedItem);
                    }
                })
                .AddTo(_CompositeDisposable);

            Observable.FromEventPattern(
                h => _watchHistoryManager.WatchHistoryAllRemoved += h,
                h => _watchHistoryManager.WatchHistoryAllRemoved -= h
                )
                .Subscribe(_ =>
                {
                    Histories.Clear();
                })
                .AddTo(_CompositeDisposable);

            base.OnNavigatedTo(parameters);
        }

        private RelayCommand _RefreshCommand;
        public RelayCommand RefreshCommand => _RefreshCommand 
            ??= new RelayCommand(async () =>
            {
                Histories.Clear();

                NowUpdating.Value = true;
                try
                {
                    var items = await _watchHistoryManager.GetWatchHistoryItemsAsync();

                    foreach (var x in items)
                    {
                        try
                        {
                            var vm = new HistoryVideoListItemControlViewModel(
                                (x.LastViewedAt ?? DateTimeOffset.Now).DateTime,
                                (uint)(x.Views ?? 0),
                                x.Video.Id,
                                x.Video.Title,
                                x.Video.Thumbnail.ListingUrl.OriginalString,
                                TimeSpan.FromSeconds(x.Video.Duration),
                                x.Video.RegisteredAt.DateTime
                                );

                            vm.ProviderId = x.Video.Owner.Id;
                            vm.ProviderType = x.Video.Owner.OwnerType switch
                            {
                                NiconicoToolkit.Video.OwnerType.User => OwnerType.User,
                                NiconicoToolkit.Video.OwnerType.Channel => OwnerType.Channel,
                                _ => OwnerType.Hidden
                            };
                            vm.ProviderName = x.Video.Owner.Name;

                            vm.CommentCount = x.Video.Count.Comment;
                            vm.ViewCount = x.Video.Count.View;
                            vm.MylistCount = x.Video.Count.Mylist;

                            Histories.Add(vm);
                        }
                        catch (Exception e)
                        {
                            _logger.ZLogErrorWithPayload(e, x, "History item process error.");
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e, "History refresh failed.");
                }
                finally
                {
                    NowUpdating.Value = false;
                }
            }
            , () => _niconicoSession.IsLoggedIn
            );
    }

    


    public class HistoryVideoListItemControlViewModel : VideoListItemControlViewModel, IWatchHistory
    {
		public DateTime LastWatchedAt { get; }
		public uint UserViewCount { get;  }

        public HistoryVideoListItemControlViewModel(DateTime lastWatchedAt, uint viewCount, string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt)
            : base(rawVideoId, title, thumbnailUrl, videoLength, postedAt)
        {
            LastWatchedAt = lastWatchedAt;
            UserViewCount = viewCount;
        }
    }

}

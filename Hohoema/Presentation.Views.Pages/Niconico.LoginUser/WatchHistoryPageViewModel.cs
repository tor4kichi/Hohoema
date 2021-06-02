using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Mntone.Nico2.Videos.Histories;
using NiconicoToolkit.Video;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.LoginUser
{
    public class WatchHistoryPageViewModel : HohoemaPageViewModelBase
	{
		public WatchHistoryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            WatchHistoryManager watchHistoryManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            WatchHistoryRemoveAllCommand watchHistoryRemoveAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
		{
            ApplicationLayoutManager = applicationLayoutManager;
            _niconicoSession = niconicoSession;
            _watchHistoryManager = watchHistoryManager;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            WatchHistoryRemoveAllCommand = watchHistoryRemoveAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            Histories = new ObservableCollection<HistoryVideoListItemControlViewModel>();
        }

        private readonly NiconicoSession _niconicoSession;
        private readonly WatchHistoryManager _watchHistoryManager;

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public WatchHistoryRemoveAllCommand WatchHistoryRemoveAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public ObservableCollection<HistoryVideoListItemControlViewModel> Histories { get; }

        HistoriesResponse _HistoriesResponse;


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (RefreshCommand.CanExecute())
            {
                RefreshCommand.Execute();
            }


            Observable.FromEventPattern<WatchHistoryRemovedEventArgs>(
                h => _watchHistoryManager.WatchHistoryRemoved += h,
                h => _watchHistoryManager.WatchHistoryRemoved -= h
                )
                .Subscribe(e =>
                {
                    var args = e.EventArgs;
                    var removedItem = Histories.FirstOrDefault(x => x.Id == args.VideoId);
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

        private DelegateCommand _RefreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                return _RefreshCommand
                    ?? (_RefreshCommand = new DelegateCommand(async () =>
                    {
                        Histories.Clear();

                        try
                        {
                            var items = await _watchHistoryManager.GetWatchHistoryItemsAsync();

                            foreach (var x in items)
                            {
                                var vm = new HistoryVideoListItemControlViewModel(
                                    x.LastViewedAt.DateTime,
                                    (uint)x.Views,
                                    x.Video.Id,
                                    x.Video.Title,
                                    x.Video.Thumbnail.ListingUrl.OriginalString,
                                    TimeSpan.FromSeconds(x.Video.Duration)
                                    );

                                vm.PostedAt = x.Video.RegisteredAt.DateTime;

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

                                await vm.InitializeAsync(default);

                                Histories.Add(vm);
                            }
                        }
                        catch (Exception e)
                        {
                            ErrorTrackingManager.TrackError(e);
                        }
                    }
                    , () => _niconicoSession.IsLoggedIn
                    ));
            }
        }



    }

    


    public class HistoryVideoListItemControlViewModel : VideoListItemControlViewModel, IWatchHistory
    {
		public DateTime LastWatchedAt { get; }
		public uint UserViewCount { get;  }

        public HistoryVideoListItemControlViewModel(DateTime lastWatchedAt, uint viewCount, string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength) : base(rawVideoId, title, thumbnailUrl, videoLength)
        {
            LastWatchedAt = lastWatchedAt;
            UserViewCount = viewCount;
        }
    }

}

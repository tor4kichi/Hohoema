using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Series;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Series;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using Hohoema.Presentation.Navigations;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Series
{
    public sealed class SeriesPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigationAware, ITitleUpdatablePage, IPinablePage
    {
        public HohoemaPin GetPin()
        {
            return new HohoemaPin()
            {
                Label = _series.Title,
                PageType = HohoemaPageType.Series,
                Parameter = $"id={_series.Id}"
            };
        }

        public IObservable<string> GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Series)
                .Select(x => x?.Title);
        }


        public SeriesPageViewModel(
            ILoggerFactory loggerFactory,
            SeriesProvider seriesRepository,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand
            )
            : base(loggerFactory.CreateLogger<SeriesPageViewModel>())
        {
            _seriesProvider = seriesRepository;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            AddSubscriptionCommand = addSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            CurrentPlaylistToken = Observable.CombineLatest(
                this.ObserveProperty(x => x.SeriesVideoPlaylist),
                this.ObserveProperty(x => x.SelectedSortOption),
                (x, y) => new PlaylistToken(x, y)
                )
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);
        }

        private readonly SeriesProvider _seriesProvider;

        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }

        private UserSeriesItemViewModel _series;
        public UserSeriesItemViewModel Series
        {
            get { return _series; }
            set { SetProperty(ref _series, value); }
        }

        private NicoVideoOwner _user;
        public NicoVideoOwner User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }

        SeriesDetails _seriesDetails;


        private SeriesVideoPlaylist _SeriesVideoPlaylist;
        public SeriesVideoPlaylist SeriesVideoPlaylist
        {
            get { return _SeriesVideoPlaylist; }
            private set { SetProperty(ref _SeriesVideoPlaylist, value); }
        }


        public SeriesPlaylistSortOption[] SortOptions => SeriesVideoPlaylist.SortOptions;


        private SeriesPlaylistSortOption _selectedSortOption;
        public SeriesPlaylistSortOption SelectedSortOption
        {
            get { return _selectedSortOption; }
            set { SetProperty(ref _selectedSortOption, value); }
        }


        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string seriesId))
            {
                _seriesDetails = await _seriesProvider.GetSeriesVideosAsync(seriesId);
                Series = new UserSeriesItemViewModel(_seriesDetails);
                User = new NicoVideoOwner()
                {
                    OwnerId = _seriesDetails.Owner.Id,
                    UserType = _seriesDetails.Owner.OwnerType,
                    ScreenName = _seriesDetails.Owner.Nickname,
                    IconUrl = _seriesDetails.Owner.IconUrl,
                };

                SeriesVideoPlaylist = new SeriesVideoPlaylist(new PlaylistId() { Id = seriesId, Origin = PlaylistItemsSourceOrigin.Series }, _seriesDetails);
                SelectedSortOption = SeriesVideoPlaylist.DefaultSortOption;

                this.ObserveProperty(x => x.SelectedSortOption).Subscribe(_ =>
                {
                    ResetList();
                })
                    .AddTo(_navigationDisposables);
            }

            await base.OnNavigatedToAsync(parameters);
        }

        protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
        {
            return (SeriesVideosIncrementalSource.OneTimeLoadingCount, new SeriesVideosIncrementalSource(SeriesVideoPlaylist, SelectedSortOption));
        }

        

        

        public class UserSeriesItemViewModel : ISeries
        {
            private readonly SeriesDetails _userSeries;

            public UserSeriesItemViewModel(SeriesDetails userSeries)
            {
                _userSeries = userSeries;
            }

            public string Id => _userSeries.Series.Id;

            public string Title => _userSeries.Series.Title;

            public bool IsListed => true;

            public string Description => _userSeries.DescriptionHTML;

            public string ThumbnailUrl => _userSeries.Series.ThumbnailUrl.OriginalString;

            public int ItemsCount => _userSeries.Videos.Count;

            public OwnerType ProviderType => _userSeries.Owner.OwnerType;

            public string ProviderId => _userSeries.Owner.Id;
        }
    }


    public class SeriesVideosIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
    {
        private SeriesDetails.SeriesOwner _owner => _seriesVideoPlaylist.SeriesDetails.Owner;
        private readonly SeriesVideoPlaylist _seriesVideoPlaylist;
        private readonly SeriesPlaylistSortOption _selectedSortOption;
        private List<SeriesDetails.SeriesVideo> _SortedItems;

        public SeriesVideosIncrementalSource(SeriesVideoPlaylist seriesVideoPlaylist, SeriesPlaylistSortOption selectedSortOption)
        {
            _seriesVideoPlaylist = seriesVideoPlaylist;
            _selectedSortOption = selectedSortOption;
        }

        public const int OneTimeLoadingCount = 25;

        Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            if (pageIndex == 0)
            {
                _SortedItems = _seriesVideoPlaylist.GetSortedItems(_selectedSortOption);
            }
            var head = pageIndex * pageSize;
            return Task.FromResult(_SortedItems.Skip(head).Take(pageSize).Select((item, i) =>
            {
                var itemVM = new VideoListItemControlViewModel(item.Id, item.Title, item.ThumbnailUrl.OriginalString, item.Duration, item.PostAt)
                {
                    PlaylistItemToken = new PlaylistItemToken(_seriesVideoPlaylist, _selectedSortOption, new SeriesVideoItem(item, _seriesVideoPlaylist.SeriesDetails.Owner))
                };

                itemVM.ViewCount = item.WatchCount;
                itemVM.CommentCount = item.CommentCount;
                itemVM.MylistCount = item.MylistCount;

                itemVM.ProviderId = _owner.Id;
                itemVM.ProviderType = _owner.OwnerType;
                itemVM.ProviderName = _owner.Nickname;

                return itemVM;
            }));
        }
    }


}

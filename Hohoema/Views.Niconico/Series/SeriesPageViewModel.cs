#nullable enable
using Hohoema.Models.Niconico.Series;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Series;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages.Niconico.Series;

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

    NvapiSeriesVidoesResponseContainer _seriesDetails;


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
                OwnerId = _seriesDetails.Data.Detail.Owner.Id,
                UserType = _seriesDetails.Data.Detail.Owner.OwnerType,
                ScreenName = _seriesDetails.Data.Detail.Owner.Name,
                IconUrl = _seriesDetails.Data.Detail.Owner.IconUrl,
            };

            SeriesVideoPlaylist = new SeriesVideoPlaylist(new PlaylistId() { Id = seriesId, Origin = PlaylistItemsSourceOrigin.Series }, _seriesDetails, _seriesProvider);
            SelectedSortOption = new SeriesPlaylistSortOption(SeriesVideoSortKey.AddedAt, PlaylistItemSortOrder.Desc);

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
        private readonly NvapiSeriesVidoesResponseContainer _userSeries;

        public UserSeriesItemViewModel(NvapiSeriesVidoesResponseContainer userSeries)
        {
            _userSeries = userSeries;
        }

        public string Id => _userSeries.Data.Detail.Id.ToString();

        public string Title => _userSeries.Data.Detail.Title;

        public bool IsListed => true;

        public string Description => _userSeries.Data.Detail.DecoratedDescriptionHtml;

        public string ThumbnailUrl => _userSeries.Data.Detail.ThumbnailUrl;

        public int ItemsCount => _userSeries.Data.TotalCount ?? 0;

        public OwnerType ProviderType => _userSeries.Data.Detail.Owner.OwnerType;

        public string ProviderId => _userSeries.Data.Detail.Owner.Id;
    }
}


public class SeriesVideosIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
{
    private readonly SeriesVideoPlaylist _seriesVideoPlaylist;
    private readonly SeriesPlaylistSortOption _selectedSortOption;
    private List<NiconicoToolkit.Series.SeriesVideoItem> _SortedItems;

    public SeriesVideosIncrementalSource(SeriesVideoPlaylist seriesVideoPlaylist, SeriesPlaylistSortOption selectedSortOption)
    {
        _seriesVideoPlaylist = seriesVideoPlaylist;
        _selectedSortOption = selectedSortOption;
    }

    public const int OneTimeLoadingCount = 25;

    private List<IVideoContent> _allItems;

    async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        if (_allItems == null)
        {
            _allItems = (await _seriesVideoPlaylist.GetAllItemsAsync(_selectedSortOption, cancellationToken)).ToList();
        }            

        return _allItems.Skip(pageIndex * pageSize).Take(pageSize).Cast<Models.Niconico.Series.SeriesVideoItem>().Select((item, i) =>
        {
            var itemVM = new VideoListItemControlViewModel(item.VideoId, item.Title, item.ThumbnailUrl, item.Length, item.PostedAt)
            {
                PlaylistItemToken = new PlaylistItemToken(_seriesVideoPlaylist, _selectedSortOption, item)
            };

            itemVM.ViewCount = item.ViewCount;
            itemVM.CommentCount = item.CommentCount;
            itemVM.MylistCount = item.MylistCount;

            itemVM.ProviderId = item.ProviderId;
            itemVM.ProviderType = item.ProviderType;
            itemVM.ProviderName = item.ProviderName;
            itemVM.IsSensitiveContent = item.RequireSensitiveMasking;
            itemVM.IsDeleted = item.IsDeleted;

            return itemVM;
        })
        .ToArray() // Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
        ;
    }
}

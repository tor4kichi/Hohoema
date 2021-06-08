using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using NiconicoToolkit.Series;
using Prism.Navigation;
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
    public sealed class SeriesPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigationAware, INavigatedAwareAsync, ITitleUpdatablePage, IPinablePage
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
            HohoemaPlaylist hohoemaPlaylist,
            SeriesProvider seriesRepository,
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            HohoemaPlaylist = hohoemaPlaylist;
            _seriesRepository = seriesRepository;
            AddSubscriptionCommand = addSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
        }

        private readonly SeriesProvider _seriesRepository;

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

        private UserSeriesItemViewModel _series;
        public UserSeriesItemViewModel Series
        {
            get { return _series; }
            set { SetProperty(ref _series, value); }
        }

        private UserViewModel _user;
        public UserViewModel User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }

        SeriesDetails _seriesDetails;


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string seriesId))
            {
                _seriesDetails = await _seriesRepository.GetSeriesVideosAsync(seriesId);
                Series = new UserSeriesItemViewModel(_seriesDetails);
                User = new UserViewModel(_seriesDetails.Owner);
            }

            await base.OnNavigatedToAsync(parameters);
        }

        protected override IIncrementalSource<VideoListItemControlViewModel> GenerateIncrementalSource()
        {
            return new SeriesVideosIncrementalSource(_seriesDetails);
        }

        

        public class UserViewModel : IUser
        {
            private readonly SeriesDetails.SeriesOwner _userDetail;

            public UserViewModel(SeriesDetails.SeriesOwner userDetail)
            {
                _userDetail = userDetail;
            }

            public string Id => _userDetail.Id;

            public string Label => _userDetail.Nickname;

            public string IconUrl => _userDetail.IconUrl;
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

            public string ProviderType => "user";

            public string ProviderId => _userSeries.Owner.Id;
        }
    }


    public class SeriesVideosIncrementalSource : HohoemaIncrementalSourceBase<VideoListItemControlViewModel>
    {
        private List<SeriesDetails.SeriesVideo> _videos => _seriesDetails.Videos;
        private SeriesDetails.SeriesOwner _owner => _seriesDetails.Owner;
        private readonly SeriesDetails _seriesDetails;

        public SeriesVideosIncrementalSource(SeriesDetails seriesDetails)
        {
            _seriesDetails = seriesDetails;
        }

        protected override ValueTask<int> ResetSourceImpl()
        {
            return new ValueTask<int>(_videos.Count);
        }

        protected override async IAsyncEnumerable<VideoListItemControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var item in _videos.Skip(head).Take(count))
            {
                var itemVM = new VideoListItemControlViewModel(item.Id, item.Title, item.ThumbnailUrl.OriginalString, item.Duration, item.PostAt);
                itemVM.ViewCount = item.WatchCount;
                itemVM.CommentCount = item.CommentCount;
                itemVM.MylistCount = item.MylistCount;

                itemVM.ProviderId = _owner.Id;
                itemVM.ProviderType = NiconicoToolkit.Video.OwnerType.User;
                itemVM.ProviderName = _owner.Nickname;

                yield return itemVM;

                ct.ThrowIfCancellationRequested();
            }
        }
    }


}

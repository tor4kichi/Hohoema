using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Series;
using NiconicoToolkit.User;
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
            SeriesProvider seriesRepository,
            VideoPlayCommand videoPlayCommand,
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            _seriesRepository = seriesRepository;
            VideoPlayCommand = videoPlayCommand;
            AddSubscriptionCommand = addSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
        }

        private readonly SeriesProvider _seriesRepository;

        public VideoPlayCommand VideoPlayCommand { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

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


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string seriesId))
            {
                _seriesDetails = await _seriesRepository.GetSeriesVideosAsync(seriesId);
                Series = new UserSeriesItemViewModel(_seriesDetails);
                User = new NicoVideoOwner()
                {
                    OwnerId = _seriesDetails.Owner.Id,
                    UserType = _seriesDetails.Owner.OwnerType,
                    ScreenName = _seriesDetails.Owner.Nickname,
                    IconUrl = _seriesDetails.Owner.IconUrl,
                };
            }

            await base.OnNavigatedToAsync(parameters);
        }

        protected override (int, IIncrementalSource<VideoListItemControlViewModel>) GenerateIncrementalSource()
        {
            return (SeriesVideosIncrementalSource.OneTimeLoadingCount, new SeriesVideosIncrementalSource(_seriesDetails));
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


    public class SeriesVideosIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
    {
        private List<SeriesDetails.SeriesVideo> _videos => _seriesDetails.Videos;
        private SeriesDetails.SeriesOwner _owner => _seriesDetails.Owner;
        private readonly SeriesDetails _seriesDetails;

        public SeriesVideosIncrementalSource(SeriesDetails seriesDetails)
        {
            _seriesDetails = seriesDetails;
        }

        public const int OneTimeLoadingCount = 25;

        Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            var head = pageIndex * pageSize;
            return Task.FromResult(_videos.Skip(head).Take(pageSize).Select(item =>
            {
                var itemVM = new VideoListItemControlViewModel(item.Id, item.Title, item.ThumbnailUrl.OriginalString, item.Duration, item.PostAt);
                itemVM.ViewCount = item.WatchCount;
                itemVM.CommentCount = item.CommentCount;
                itemVM.MylistCount = item.MylistCount;

                itemVM.ProviderId = _owner.Id;
                itemVM.ProviderType = NiconicoToolkit.Video.OwnerType.User;
                itemVM.ProviderName = _owner.Nickname;

                return itemVM;
            }));
        }
    }


}

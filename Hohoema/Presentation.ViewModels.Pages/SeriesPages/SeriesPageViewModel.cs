using Mntone.Nico2.Videos.Series;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.UseCase.NicoVideos;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Hohoema.Presentation.ViewModels.Subscriptions.Commands;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;

namespace Hohoema.Presentation.ViewModels.Pages.SeriesPages
{
    public sealed class SeriesPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigationAware, INavigatedAwareAsync, ITitleUpdatablePage, IPinablePage
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
            SeriesRepository seriesRepository,
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            HohoemaPlaylist = hohoemaPlaylist;
            _seriesRepository = seriesRepository;
            AddSubscriptionCommand = addSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
        }

        private readonly SeriesRepository _seriesRepository;

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

        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
        {
            return new SeriesVideosIncrementalSource(_seriesDetails.Videos);
        }

        

        public class UserViewModel : IUser
        {
            private readonly SeriesOwner _userDetail;

            public UserViewModel(SeriesOwner userDetail)
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


    public class SeriesVideosIncrementalSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
    {
        private List<SeriresVideo> _videos;

        public SeriesVideosIncrementalSource(List<SeriresVideo> videos)
        {
            _videos = videos;
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult(_videos.Count);
        }

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var item in _videos.Skip(head).Take(count))
            {
                var itemVM = new VideoInfoControlViewModel(item.Id);
                itemVM.SetTitle(item.Title);
                itemVM.SetSubmitDate(item.PostAt);
                itemVM.SetVideoDuration(item.Duration);
                itemVM.SetDescription(item.WatchCount, item.CommentCount, item.MylistCount);
                itemVM.SetThumbnailImage(item.ThumbnailUrl.OriginalString);
                yield return itemVM;

                //_ = itemVM.InitializeAsync(ct);

                ct.ThrowIfCancellationRequested();
            }
        }
    }


}

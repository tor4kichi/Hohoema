using Mntone.Nico2.Users.Series;
using Mntone.Nico2.Videos.Series;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Repository.NicoVideo;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.ViewModels.Subscriptions;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class SeriesPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigationAware, INavigatedAwareAsync, Interfaces.ITitleUpdatablePage, IPinablePage
    {
        public HohoemaPin GetPin()
        {
            return new HohoemaPin()
            {
                Label = _series.Title,
                PageType = Services.HohoemaPageType.Series,
                Parameter = $"id={_series.Id}"
            };
        }

        public IObservable<string> GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Series)
                .Select(x => x?.Title);
        }


        public SeriesPageViewModel(
            SeriesRepository seriesRepository,
            AddSubscriptionCommand addSubscriptionCommand
            )
        {
            _seriesRepository = seriesRepository;
            AddSubscriptionCommand = addSubscriptionCommand;
        }

        private readonly SeriesRepository _seriesRepository;
        public AddSubscriptionCommand AddSubscriptionCommand { get; }

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

            public SeriesProviderType ProviderType => SeriesProviderType.User;

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

        protected override Task<IAsyncEnumerable<VideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(_videos.Skip(head).Take(count)
                .Select(x =>
                {
                    var itemVM = new VideoInfoControlViewModel(x.Id);
                    itemVM.SetTitle(x.Title);
                    itemVM.SetSubmitDate(x.PostAt);
                    itemVM.SetVideoDuration(x.Duration);
                    itemVM.SetDescription(x.WatchCount, x.CommentCount, x.MylistCount);
                    itemVM.SetThumbnailImage(x.ThumbnailUrl.OriginalString);
                    return itemVM;
                })
                .ToAsyncEnumerable());
        }
    }


}

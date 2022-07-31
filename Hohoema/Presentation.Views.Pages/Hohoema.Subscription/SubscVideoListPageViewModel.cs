using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.Navigations;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Subscription
{
    public partial class SubscVideoListPageViewModel : HohoemaListingPageViewModelBase<SubscVideoListItemViewModel>
    {
        private readonly PageManager _pageManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public SubscVideoListPageViewModel(
            ILogger logger,
            PageManager pageManager,
            SubscriptionManager subscriptionManager,
            NicoVideoProvider nicoVideoProvider
            )
            : base(logger)
        {
            _pageManager = pageManager;
            _subscriptionManager = subscriptionManager;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            _subscriptionManager.Updated += _subscriptionManager_Updated;
        }

        private void _subscriptionManager_Updated(object sender, SubscriptionFeedUpdateResult e)
        {
            ResetList();
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _subscriptionManager.Updated -= _subscriptionManager_Updated;
        }

        protected override (int PageSize, IIncrementalSource<SubscVideoListItemViewModel> IncrementalSource) GenerateIncrementalSource()
        {
            return (SubscVideoListIncrementalLoadingSource.PageSize, new SubscVideoListIncrementalLoadingSource(_subscriptionManager, _nicoVideoProvider));
        }


        [RelayCommand]
        public void OpenSubscManagementPage()
        {
            _pageManager.OpenPage(Models.Domain.PageNavigation.HohoemaPageType.SubscriptionManagement);
        }


        [RelayCommand]
        public void AllChecked()
        {
            var items = _subscriptionManager.GetUncheckedSubscFeedVideos().ToArray();           
            foreach (var itemVM in items)
            {
                itemVM.IsChecked = true;
            }

            _subscriptionManager.UpdateFeedVideos(items);

            ResetList();
        }
    }

    public sealed class SubscVideoListIncrementalLoadingSource : IIncrementalSource<SubscVideoListItemViewModel>
    {
        private readonly SubscriptionManager _subscriptionManager;
        private readonly NicoVideoProvider _nicoVideoProvider;
        public const int PageSize = 20;

        public SubscVideoListIncrementalLoadingSource(
            SubscriptionManager subscriptionManager,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _subscriptionManager = subscriptionManager;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public Task<IEnumerable<SubscVideoListItemViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var videos = _subscriptionManager.GetUncheckedSubscFeedVideos(pageIndex * pageSize, pageSize);

            List<SubscVideoListItemViewModel> resultItems = new();
            foreach (var video in videos)
            {
                var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(video.VideoId);
                resultItems.Add(new SubscVideoListItemViewModel(video, nicoVideo));
            }

            return Task.FromResult<IEnumerable<SubscVideoListItemViewModel>>(resultItems);
        }
    }


    public partial class SubscVideoListItemViewModel : VideoListItemControlViewModel
    {
        public SubscVideoListItemViewModel(SubscFeedVideo feedVideo, NicoVideo video) : base(video)
        {
            FeedVideo = feedVideo;
            _isChecked = feedVideo.IsChecked;
        }

        public SubscFeedVideo FeedVideo { get; }

        [ObservableProperty]
        private bool _isChecked;


        partial void OnIsCheckedChanged(bool value)
        {
            FeedVideo.IsChecked = value;
        }
    }
}

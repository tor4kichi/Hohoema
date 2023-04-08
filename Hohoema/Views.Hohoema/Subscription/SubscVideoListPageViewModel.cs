using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription
{
    public partial class SubscVideoListPageViewModel : HohoemaListingPageViewModelBase<SubscVideoListItemViewModel>
    {
        private readonly IMessenger _messenger;
        private readonly PageManager _pageManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

        public SubscVideoListPageViewModel(
            ILogger logger,
            IMessenger messenger,
            PageManager pageManager,
            SubscriptionManager subscriptionManager,
            NicoVideoProvider nicoVideoProvider,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            ApplicationLayoutManager applicationLayoutManager,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
            : base(logger)
        {
            _messenger = messenger;
            _pageManager = pageManager;
            _subscriptionManager = subscriptionManager;
            _nicoVideoProvider = nicoVideoProvider;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            ApplicationLayoutManager = applicationLayoutManager;
            SelectionModeToggleCommand = selectionModeToggleCommand;
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            _messenger.Register<SubscFeedVideoValueChangedMessage>(this, (r, m) => 
            {
                //if (m.Value.IsChecked is false)
                //{
                //    var target = ItemsView.Cast<SubscVideoListItemViewModel>().FirstOrDefault(x => x.VideoId == m.Value.VideoId);
                //    if (target is not null)
                //    {
                //        ItemsView.Remove(target);
                //    }
                //}
            });

            _messenger.Register<NewSubscFeedVideoMessage>(this, (r, m) => 
            {
                var feed = m.Value;
                VideoId videoId = feed.VideoId;

                if (ItemsView.Cast<SubscVideoListItemViewModel>().FirstOrDefault(x => x.VideoId == videoId) is not null)
                {
                    return;
                }

                var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
                var itemVM = new SubscVideoListItemViewModel(feed, nicoVideo);
                ItemsView.Insert(0, itemVM);
            });
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _messenger.Unregister<SubscFeedVideoValueChangedMessage>(this);
            _messenger.Unregister<NewSubscFeedVideoMessage>(this);
        }

        protected override (int PageSize, IIncrementalSource<SubscVideoListItemViewModel> IncrementalSource) GenerateIncrementalSource()
        {
            return (SubscVideoListIncrementalLoadingSource.PageSize, new SubscVideoListIncrementalLoadingSource(_subscriptionManager, _nicoVideoProvider));
        }


        [RelayCommand]
        public void OpenSubscManagementPage()
        {
            _pageManager.OpenPage(Models.PageNavigation.HohoemaPageType.SubscriptionManagement);
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

        HashSet<VideoId> _videoIds = new HashSet<VideoId>();

        public Task<IEnumerable<SubscVideoListItemViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var videos = _subscriptionManager.GetSubscFeedVideos(pageIndex * pageSize, pageSize);

            List<SubscVideoListItemViewModel> resultItems = new();
            foreach (var video in videos)
            {
                VideoId videoId = video.VideoId;
                if (_videoIds.Contains(videoId))
                {
                    continue;
                }

                _videoIds.Add(videoId);
                var nicoVideo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
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
        }

        public SubscFeedVideo FeedVideo { get; }
    }
}

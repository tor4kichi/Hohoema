#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Video;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription;

public partial class SubscVideoListPageViewModel : HohoemaListingPageViewModelBase<SubscVideoListItemViewModel>
{
    private readonly IMessenger _messenger;
    private readonly PageManager _pageManager;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly NicoVideoProvider _nicoVideoProvider;

    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

    public ObservableCollection<SubscriptionGroup> SubscriptionGroups { get; }

    [ObservableProperty]
    private SubscriptionGroup? _selectedSubscGroup;

    partial void OnSelectedSubscGroupChanged(SubscriptionGroup? value)
    {
        if (_lastSelectedSubscGroup != value)
        {
            ResetList();
        }
        _lastSelectedSubscGroup = value;
    }

    private SubscriptionGroup _defaultSubscGroup;
    private SubscriptionGroup? _lastSelectedSubscGroup;
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
        SubscriptionGroups = new(_subscriptionManager.GetSubscGroups());
        _defaultSubscGroup = new SubscriptionGroup(ObjectId.Empty, "SubscGroup_DefaultGroupName".Translate());
    }

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        SubscriptionGroups.Clear();
        SubscriptionGroups.Add(_defaultSubscGroup);
        foreach (var subscGroup in _subscriptionManager.GetSubscGroups())
        {
            SubscriptionGroups.Add(subscGroup);
        }
        
        try
        {
            if (parameters.TryGetValue("SubscGroupId", out string idStr))
            {
                ObjectId id = new ObjectId(idStr);
                if (SubscriptionGroups.FirstOrDefault(x => x.Id == id) is not null and var group)
                {
                    SelectedSubscGroup = group;
                }
                else
                {
                    SelectedSubscGroup = _defaultSubscGroup;
                }
            }
            else
            {
                SelectedSubscGroup = _defaultSubscGroup;
            }
        }
        catch 
        {
            SelectedSubscGroup = _defaultSubscGroup;
        }

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
        return (SubscVideoListIncrementalLoadingSource.PageSize, new SubscVideoListIncrementalLoadingSource(SelectedSubscGroup, _subscriptionManager, _nicoVideoProvider));
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
        SubscriptionGroup? subscriptionGroup,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider
        )
    {
        SubscriptionGroup = subscriptionGroup;
        _subscriptionManager = subscriptionManager;
        _nicoVideoProvider = nicoVideoProvider;
    }

    HashSet<VideoId> _videoIds = new HashSet<VideoId>();

    public SubscriptionGroup? SubscriptionGroup { get; }

    public Task<IEnumerable<SubscVideoListItemViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var videos = SubscriptionGroup != null
            ? _subscriptionManager.GetSubscFeedVideos(SubscriptionGroup, pageIndex * pageSize, pageSize)
            : _subscriptionManager.GetSubscFeedVideos(pageIndex * pageSize, pageSize)
            ;

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

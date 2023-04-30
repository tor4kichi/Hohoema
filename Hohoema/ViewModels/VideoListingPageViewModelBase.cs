using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Playlist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Services.VideoCache.Events;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels;
public abstract class VideoListingPageViewModelBase<ITEM_VM> 
    : HohoemaListingPageViewModelBase<ITEM_VM>
    , IRecipient<VideoWatchedMessage>
    , IRecipient<PlaylistItemAddedMessage>
    , IRecipient<PlaylistItemRemovedMessage>
    , IRecipient<ItemIndexUpdatedMessage>
    , IRecipient<VideoCacheStatusChangedMessage>
    , IRecipient<VideoOwnerFilteringAddedMessage>
    , IRecipient<VideoOwnerFilteringRemovedMessage>
    where ITEM_VM : VideoItemViewModel
{
    protected readonly IMessenger _messenger;

    protected VideoListingPageViewModelBase(
        IMessenger messenger,
        ILogger logger,
        bool disposeItemVM
        ) : base(logger, disposeItemVM)
    {
        _messenger = messenger;
    }

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        _messenger.Register<VideoWatchedMessage>(this);
        _messenger.Register<PlaylistItemAddedMessage>(this);
        _messenger.Register<PlaylistItemRemovedMessage>(this);
        _messenger.Register<ItemIndexUpdatedMessage>(this);
        _messenger.Register<VideoCacheStatusChangedMessage>(this);

        if (typeof(VideoListItemControlViewModel).IsAssignableFrom(typeof(ITEM_VM)))
        {
            _messenger.Register<VideoOwnerFilteringAddedMessage>(this);
            _messenger.Register<VideoOwnerFilteringRemovedMessage>(this);
        }

        try
        {
            base.OnNavigatedTo(parameters);
        }
        catch
        {
            _messenger.Unregister<VideoWatchedMessage>(this);
            _messenger.Unregister<PlaylistItemAddedMessage>(this);
            _messenger.Unregister<PlaylistItemRemovedMessage>(this);
            _messenger.Unregister<ItemIndexUpdatedMessage>(this);
            _messenger.Unregister<VideoCacheStatusChangedMessage>(this);
            _messenger.Unregister<VideoOwnerFilteringAddedMessage>(this);
            _messenger.Unregister<VideoOwnerFilteringRemovedMessage>(this);
            throw;
        }
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<VideoWatchedMessage>(this);
        _messenger.Unregister<PlaylistItemAddedMessage>(this);
        _messenger.Unregister<PlaylistItemRemovedMessage>(this);
        _messenger.Unregister<ItemIndexUpdatedMessage>(this);
        _messenger.Unregister<VideoCacheStatusChangedMessage>(this);
        _messenger.Unregister<VideoOwnerFilteringAddedMessage>(this);
        _messenger.Unregister<VideoOwnerFilteringRemovedMessage>(this);
        base.OnNavigatedFrom(parameters);
    }

    public static IEnumerable<T> ToTypedVideoItemVMEnumerable<T>(IEnumerable items) where T : VideoItemViewModel
    {
        foreach (var item in items)
        {
            if (item is T videoItemVM)
            {
                yield return videoItemVM;
            }
        }
    }

    static IEnumerable<VideoItemViewModel> ToVideoItemVMEnumerable(IEnumerable items)
    {
        foreach (var item in items)
        {
            if (item is VideoItemViewModel videoItemVM)
            {
                yield return videoItemVM;
            }
        }
    }

    void IRecipient<VideoWatchedMessage>.Receive(VideoWatchedMessage message)
    {
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnWatched(message);
        }
    }

    void IRecipient<PlaylistItemAddedMessage>.Receive(PlaylistItemAddedMessage message)
    {
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnPlaylistItemAdded(message);
        }
    }

    void IRecipient<PlaylistItemRemovedMessage>.Receive(PlaylistItemRemovedMessage message)
    {
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnPlaylistItemRemoved(message);
        }
    }

    void IRecipient<ItemIndexUpdatedMessage>.Receive(ItemIndexUpdatedMessage message)
    {
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnQueueItemIndexUpdated(message);
        }
    }

    void IRecipient<VideoCacheStatusChangedMessage>.Receive(VideoCacheStatusChangedMessage message)
    {
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnCacheStatusChanged(message);
        }
    }

    void IRecipient<VideoOwnerFilteringAddedMessage>.Receive(VideoOwnerFilteringAddedMessage message)
    {
        foreach (var videoItemVM in ToTypedVideoItemVMEnumerable<VideoListItemControlViewModel>(ItemsView.SourceCollection))
        {
            videoItemVM.OnVideoOwnerFilteringAdded(message);
        }
    }

    void IRecipient<VideoOwnerFilteringRemovedMessage>.Receive(VideoOwnerFilteringRemovedMessage message)
    {
        foreach (var videoItemVM in ToTypedVideoItemVMEnumerable<VideoListItemControlViewModel>(ItemsView.SourceCollection))
        {
            videoItemVM.OnVideoOwnerFilteringRemoved(message);
        }
    }
}

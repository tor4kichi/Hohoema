using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Video;
using Hohoema.ViewModels.Niconico.Follow;
using NiconicoToolkit;
using NiconicoToolkit.Channels;
using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Flyouts;

public class ChannelViewModel : IChannel
{
    public ChannelId ChannelId { get; init; }

    public string Name { get; init; }
}
public sealed partial class ChannelInfoFlyout : Microsoft.UI.Xaml.Controls.CommandBarFlyout
{        
    
    public static async Task<ChannelInfoFlyout?> CreateAsync(ChannelId channelId)
    {
        var userFollowProvider = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ChannelFollowProvider>();
        var info = await userFollowProvider.GetChannelAuthorityAsync(channelId);

        if (!info.IsSuccess) { return null; }
        var flyout = new ChannelInfoFlyout()
        {
            ChannelId = channelId,
            Name = info.Data.Channel.Name,
        };
        flyout.SetIcon(info.Data.Channel.ThumbnailSmallUrl);
        
        if (info.Data.Session is not null and var session)
        {
            flyout.UserFollowContext = FollowContext<IChannel>.Create(userFollowProvider, new ChannelViewModel() { ChannelId = channelId, Name = info.Data.Channel.Name }, session.IsFollowing);
        }

        return flyout;
    }


    public string Name
    {
        get => NameTextBlock.Text;
        set => NameTextBlock.Text = value;
    }


    private ChannelId? _channelId;
    public ChannelId? ChannelId
    {
        get => _channelId;
        set
        {
            if (_channelId != value)
            {
                _channelId = value;
                if (_channelId != null)
                {
                    FilterToggleButton.IsChecked = _filterSettings.IsHiddenVideoOwnerId(_channelId);
                }
            }
        }
    }

    public void SetIcon(Uri uri)
    {
        ChannelIconImage.Source = new BitmapImage(uri);
    }

    FollowContext<IChannel> _ChannelFollowContext;
    public FollowContext<IChannel> UserFollowContext
    {
        get => _ChannelFollowContext;
        set
        {
            if (_ChannelFollowContext != value)
            {
                _ChannelFollowContext = value;
                if (_ChannelFollowContext is not null && _ChannelFollowContext != FollowContext<IChannel>.Default)
                {
                    FollowToggleButton.IsChecked = _ChannelFollowContext.IsFollowing;
                }
            }
        }
    }

    private readonly PageManager _pageManager;
    private readonly VideoFilteringSettings _filterSettings;

    ChannelInfoFlyout()
    {
        this.InitializeComponent();

        _pageManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<PageManager>();
        _filterSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoFilteringSettings>();
    }




    private void OpenChannelVideoButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChannelId is not null and ChannelId channelId)
        {
            _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.ChannelVideo, channelId);
        }
    }

    private async void OpenWithBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChannelId is not null and ChannelId channelId)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri(NiconicoUrls.MakeChannelPageUrl(channelId)));
            }
            catch { }
        }
    }

    private async void FollowToggleButton_Click(object sender, RoutedEventArgs e)
    {
        await _ChannelFollowContext.ToggleFollowAsync();

        FollowToggleButton.IsChecked = _ChannelFollowContext.IsFollowing;
    }

    private void FilterToggleButton_Click(object sender, RoutedEventArgs e)
    {
        
        if (_filterSettings.IsHiddenVideoOwnerId(ChannelId ?? throw new NullReferenceException(nameof(ChannelId))))
        {
            _filterSettings.RemoveHiddenVideoOwnerId(ChannelId.Value);
        }
        else
        {
            _filterSettings.AddHiddenVideoOwnerId(ChannelId.Value, Name);
        }
    }
}

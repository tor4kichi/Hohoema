﻿#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Video;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.ViewModels.Pages.Niconico.User;
using NiconicoToolkit;
using NiconicoToolkit.User;
using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.User;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class UserInfoFlyout : Microsoft.UI.Xaml.Controls.CommandBarFlyout
{
    public static async Task<UserInfoFlyout> CreateAsync(UserId userId)
    {
        var niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NiconicoSession>();
        var info = await niconicoSession.ToolkitContext.User.GetUserDetailAsync(userId);
        if (!info.IsSuccess) { return null; }
        var user = info.Data.User;
        var flyout = new UserInfoFlyout()
        {
            UserId = user.Id,
            Nickname = user.Nickname,
        };
        flyout.SetIcon(user.Icons.Small);
        var userFollowProvider = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<UserFollowProvider>();
        flyout.UserFollowContext = FollowContext<IUser>.Create(userFollowProvider, new UserViewModel() { UserId = user.Id, Nickname = user.Nickname, IconUrl = user.Icons.Small.OriginalString }, info.Data.Relationships.SessionUser.IsFollowing);
        return flyout;
    }        


    public string Nickname
    {
        get => NicknameTextBlock.Text;
        set => NicknameTextBlock.Text = value;
    }


    private UserId? _userId;
    public UserId? UserId
    {
        get => _userId;
        set
        {
            if (_userId != value)
            {
                _userId = value;
                if (_userId != null)
                {
                    FilterToggleButton.IsChecked = _filterSettings.IsHiddenVideoOwnerId(_userId);
                }
            }
        }
    }

    public void SetIcon(Uri uri)
    {
        UserIconImage.Source = new BitmapImage(uri);
    }

    FollowContext<IUser>? _UserFollowContext;
    public FollowContext<IUser>? UserFollowContext 
    {
        get => _UserFollowContext;
        set
        {
            if (_UserFollowContext != value)
            {
                _UserFollowContext = value;
                if (_UserFollowContext is not null && _UserFollowContext != FollowContext<IUser>.Default)
                {
                    FollowToggleButton.IsChecked = _UserFollowContext.IsFollowing;
                }
            }
        }
    }

    private readonly IMessenger _messenger;
    private readonly VideoFilteringSettings _filterSettings;

    UserInfoFlyout()
    {
        this.InitializeComponent();

        _messenger  = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IMessenger>();
        _filterSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<VideoFilteringSettings>();
    }


    private void OpenUserVideoButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserId is not null and UserId userId)
        {
            _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserVideo, userId);
        }
    }

    private void OpenUserMylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserId is not null and UserId userId)
        {
            _ =  _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserMylist, userId);
        }
    }

    private void OpenUserSeriesButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserId is not null and UserId userId)
        {
            _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserSeries, userId);
        }
    }

    private async void OpenWithBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserId is not null and UserId userId)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri(NiconicoUrls.MakeUserPageUrl(userId)));
            }
            catch { }
        }
    }

    private async void FollowToggleButton_Click(object sender, RoutedEventArgs e)
    {
        await _UserFollowContext.ToggleFollowAsync();

        FollowToggleButton.IsChecked = _UserFollowContext.IsFollowing;
    }

    private void FilterToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_filterSettings.IsHiddenVideoOwnerId(_userId))
        {
            _filterSettings.RemoveHiddenVideoOwnerId(UserId.Value);
        }
        else
        {
            _filterSettings.AddHiddenVideoOwnerId(UserId.Value, Nickname);
        }
    }
}

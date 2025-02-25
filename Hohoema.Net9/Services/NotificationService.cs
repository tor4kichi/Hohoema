#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Notification;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Contracts.Player;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using NiconicoToolkit;
using NiconicoToolkit.Live;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Services;

public sealed class HohoemaNotificationService
{
    private static readonly TimeSpan DefaultNotificationShowDuration = TimeSpan.FromSeconds(20);

    public NotificationService NotificationService { get; }
    public NicoVideoProvider NicoVideoProvider { get; }
    public MylistProvider MylistProvider { get; }
    public NicoLiveProvider NicoLiveProvider { get; }
    public UserProvider UserProvider { get; }

    private readonly IMessenger _messenger;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;
    private readonly NiconicoSession _niconicoSession;

    public HohoemaNotificationService(
        QueuePlaylist queuePlaylist,
        HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
        NiconicoSession niconicoSession,
        NotificationService notificationService,
        NicoVideoProvider nicoVideoProvider,
        MylistProvider mylistProvider,
        NicoLiveProvider nicoLiveProvider,
        UserProvider userProvider,
        IMessenger messenger
        )
    {
        _queuePlaylist = queuePlaylist;
        _niconicoSession = niconicoSession;
        NotificationService = notificationService;
        NicoVideoProvider = nicoVideoProvider;
        MylistProvider = mylistProvider;
        NicoLiveProvider = nicoLiveProvider;
        UserProvider = userProvider;

        _messenger = messenger;
    }


    public async void ShowInAppNotification(NiconicoId id)
    {
        Task<InAppNotificationPayload> notificationPayload = null;
        switch (id.IdType)
        {
            case NiconicoIdType.Video:
                notificationPayload = SubmitVideoContentSuggestion((VideoId)id);
                break;
            case NiconicoIdType.Live:
                notificationPayload = SubmitLiveContentSuggestion((LiveId)id);
                break;
            case NiconicoIdType.Mylist:
                notificationPayload = SubmitMylistContentSuggestion((MylistId)id);
                break;
            case NiconicoIdType.User:
                notificationPayload = SubmitUserSuggestion((UserId)id);
                break;
            case NiconicoIdType.Channel:
                //notificationPayload = SubmitChannelSuggestion(id);
                break;
            case NiconicoIdType.Series:
                notificationPayload = SubmitSeriesSuggestion(id);
                break;
            default:
                break;
        }

        if (notificationPayload != null)
        {
            NotificationService.ShowInAppNotification(await notificationPayload);
        }
    }



    private async Task<InAppNotificationPayload> SubmitVideoContentSuggestion(VideoId videoId)
    {
        var (res, nicoVideo) = await NicoVideoProvider.GetVideoInfoAsync(videoId);

        if (res.IsOK is false || string.IsNullOrEmpty(nicoVideo.Title)) { return null; }

        return new InAppNotificationPayload()
        {
            Content = "InAppNotification_ContentDetectedFromClipboard".Translate(nicoVideo.Title),
            ShowDuration = DefaultNotificationShowDuration,
            IsShowDismissButton = true,
            Commands = {
                    new InAppNotificationCommand()
                    {
                        Label = "Play".Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.Send(VideoPlayRequestMessage.PlayVideo(videoId));

                            NotificationService.DismissInAppNotification();
                        })
                    },
                    new InAppNotificationCommand()
                    {
                        Label = "@view".Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _queuePlaylist.Add(nicoVideo);

                            NotificationService.DismissInAppNotification();
                        })
                    },
                    new InAppNotificationCommand()
                    {
                        Label = HohoemaPageType.VideoInfomation.Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.OpenPageWithIdAsync(HohoemaPageType.VideoInfomation, videoId);

                            NotificationService.DismissInAppNotification();
                        })
                    },
                }
        };
    }

    private async Task<InAppNotificationPayload> SubmitLiveContentSuggestion(LiveId liveId)
    {
        var liveDesc = await NicoLiveProvider.GetLiveInfoAsync(liveId);

        if (liveDesc == null) { return null; }

        var liveTitle = liveDesc.Data.Title;

        var payload = new InAppNotificationPayload()
        {
            Content = "InAppNotification_ContentDetectedFromClipboard".Translate(liveTitle),
            ShowDuration = DefaultNotificationShowDuration,
            IsShowDismissButton = true,
            Commands = {
                    new InAppNotificationCommand()
                    {
                        Label = "WatchLiveStreaming".Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.Send(new PlayLiveRequestMessage(liveId));

                            NotificationService.DismissInAppNotification();
                        })
                    },
                    new InAppNotificationCommand()
                    {
                        Label = HohoemaPageType.LiveInfomation.Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.OpenPageWithIdAsync(HohoemaPageType.LiveInfomation, liveId);

                            NotificationService.DismissInAppNotification();
                        })
                    },

                }
        };

        if (liveDesc.Data.ProviderType == ProviderType.Community)
        {
            payload.Commands.Add(new InAppNotificationCommand()
            {
                Label = HohoemaPageType.Community.Translate(),
                Command = new RelayCommand(() =>
                {
                    _messenger.OpenPageWithIdAsync(HohoemaPageType.Community, liveDesc.Data.ProviderId);

                    NotificationService.DismissInAppNotification();
                })
            });
        }

        return payload;
    }

    private async Task<InAppNotificationPayload> SubmitMylistContentSuggestion(MylistId mylistId)
    {
        MylistPlaylist mylistDetail = null;
        try
        {
            mylistDetail = await MylistProvider.GetMylist(mylistId);
        }
        catch { }

        if (mylistDetail == null) { return null; }

        return new InAppNotificationPayload()
        {
            Content = "InAppNotification_ContentDetectedFromClipboard".Translate(mylistDetail.Name),
            ShowDuration = DefaultNotificationShowDuration,
            IsShowDismissButton = true,
            Commands = {
                    new InAppNotificationCommand()
                    {
                        Label = HohoemaPageType.Mylist.Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.OpenPageWithIdAsync(HohoemaPageType.Mylist, mylistId);

                            NotificationService.DismissInAppNotification();
                        })
                    },
                }
        };
    }

    private async Task<InAppNotificationPayload> SubmitUserSuggestion(UserId userId)
    {
        var user = await UserProvider.GetUserInfoAsync(userId);

        if (user == null) { return null; }

        return new InAppNotificationPayload()
        {
            Content = "InAppNotification_ContentDetectedFromClipboard".Translate(user.ScreenName),
            ShowDuration = DefaultNotificationShowDuration,
            IsShowDismissButton = true,
            Commands = {
                    new InAppNotificationCommand()
                    {
                        Label = HohoemaPageType.UserInfo.Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, userId);

                            NotificationService.DismissInAppNotification();
                        })
                    },
                    new InAppNotificationCommand()
                    {
                        Label = HohoemaPageType.UserVideo.Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.OpenPageWithIdAsync(HohoemaPageType.UserVideo, userId);

                            NotificationService.DismissInAppNotification();
                        })
                    },
                }
        };
    }
    /*
    private async Task<InAppNotificationPayload> SubmitChannelSuggestion(string channelIdOrScreenName)
    {
        var channel = await _niconicoSession.ToolkitContext.Channel.GetChannelVideoAsync()
    }
    */
    private async Task<InAppNotificationPayload> SubmitSeriesSuggestion(string seriesId)
    {
        var series = await _niconicoSession.ToolkitContext.Series.GetUserSeriesVideosAsync(seriesId);

        if (!(series.Data.Items?.Any() ?? false))
        {
            return null;
        }

        return new InAppNotificationPayload()
        {
            Content = "InAppNotification_ContentDetectedFromClipboard".Translate(series.Data.Detail.Title),
            ShowDuration = DefaultNotificationShowDuration,
            IsShowDismissButton = true,
            Commands = {
                    new InAppNotificationCommand()
                    {
                        Label = HohoemaPageType.Series.Translate(),
                        Command = new RelayCommand(() =>
                        {
                            _messenger.OpenPageWithIdAsync(HohoemaPageType.Series, seriesId);

                            NotificationService.DismissInAppNotification();
                        })
                    }
                }
        };
    }
}

public sealed class NotificationService : INotificationService
{
    private readonly ToastNotifier _notifier;
    private ToastNotification _prevToastNotification;
    private readonly IMessenger _messenger;

    public NotificationService(IMessenger messenger)
    {
        _messenger = messenger;
        _notifier = ToastNotificationManager.CreateToastNotifier();
        ToastNotificationManager.History.Clear();
    }


    public void ShowToast(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false, string luanchContent = null, Action toastActivatedAction = null)
    {
        var toust = new ToastContent();
        toust.Visual = new ToastVisual()
        {
            BindingGeneric = new ToastBindingGeneric()
            {
                Children =
                {
                    new AdaptiveText()
                    {
                        Text = title
                    },

                    new AdaptiveText()
                    {
                        Text = content,

                    },


                }
            }
        };

        toust.Launch = luanchContent;
        toust.Duration = duration;


        var toast = new ToastNotification(toust.GetXml());
        toast.SuppressPopup = isSuppress;

        if (toastActivatedAction != null)
        {
            toast.Activated += (ToastNotification sender, object args) => toastActivatedAction();
        }

        _notifier.Show(toast);
        _prevToastNotification = toast;
    }


    public void ShowToast(
        string title,
        string content,
        ToastDuration duration = ToastDuration.Short,
        bool isSuppress = false,
        string? luanchContent = null,
        Action? toastActivatedAction = null,
        IToastButton[]? toastButtons = null,
        IToastInput[]? toastInputs = null,
        ToastContextMenuItem[]? toastMenuItems = null
        )
    {
        var toust = new ToastContent();
        toust.Visual = new ToastVisual()
        {
            BindingGeneric = new ToastBindingGeneric()
            {
                Children =
                {
                    new AdaptiveText()
                    {
                        Text = title
                    },

                    new AdaptiveText()
                    {
                        Text = content,

                    },
                }
            }
        };

        toust.Launch = luanchContent;
        toust.Duration = duration;

        if (toastButtons.Any())
        {
            var actions = new ToastActionsCustom();
            if (toastButtons is not null)
            {
                foreach (var button in toastButtons)
                {
                    actions.Buttons.Add(button);
                }
            }
            if (toastInputs is not null)
            {
                foreach (var input in toastInputs)
                {
                    actions.Inputs.Add(input);
                }
            }
            if (toastMenuItems is not null)
            {
                foreach (var menuItem in toastMenuItems)
                {
                    actions.ContextMenuItems.Add(menuItem);
                }
            }
            toust.Actions = actions;
        }

        var toast = new ToastNotification(toust.GetXml());
        toast.SuppressPopup = isSuppress;

        if (toastActivatedAction != null)
        {
            toast.Activated += (ToastNotification sender, object args) => toastActivatedAction();
        }

        _notifier.Show(toast);
        _prevToastNotification = toast;
    }


    public void ShowInAppNotification(InAppNotificationPayload payload)
    {
        _messenger.Send(new InAppNotificationMessage(payload));
    }



    public void DismissInAppNotification()
    {
        _messenger.Send(new InAppNotificationDismissMessage());
    }

    public void HideToast()
    {
        ToastNotificationManager.History.Clear();
    }


    public void ShowLiteInAppNotification_Success(string content, DisplayDuration? displayDuration = null)
    {
        ShowLiteInAppNotification(content, displayDuration, Symbol.Accept);
    }

    public void ShowLiteInAppNotification_Success(string content, TimeSpan duration)
    {
        ShowLiteInAppNotification(content, duration, Symbol.Accept);
    }

    public void ShowLiteInAppNotification_Fail(string content, DisplayDuration? displayDuration = null)
    {
        ShowLiteInAppNotification(content, displayDuration ?? DisplayDuration.MoreAttention, Symbol.Important);
    }

    public void ShowLiteInAppNotification_Fail(string content, TimeSpan duration)
    {
        ShowLiteInAppNotification(content, duration, Symbol.Important);
    }


    public void ShowLiteInAppNotification(string content, DisplayDuration? displayDuration = null, Symbol? symbol = null)
    {
        _messenger.Send(new LiteNotificationMessage(new()
        {
            Content = content,
            Symbol = symbol ?? default,
            IsDisplaySymbol = symbol is not null,
            DisplayDuration = displayDuration
        }));
    }

    public void ShowLiteInAppNotification(string content, TimeSpan duration, Symbol? symbol = null)
    {
        _messenger.Send(new LiteNotificationMessage(new()
        {
            Content = content,
            Symbol = symbol ?? default,
            IsDisplaySymbol = symbol is not null,
            Duration = duration
        }));
    }

}

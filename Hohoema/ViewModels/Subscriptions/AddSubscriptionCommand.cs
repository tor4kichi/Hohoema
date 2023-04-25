#nullable enable
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.ViewModels.Pages.Hohoema.Subscription;
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Video;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZLogger;
using static System.Net.Mime.MediaTypeNames;

namespace Hohoema.ViewModels.Subscriptions;

public sealed class AddSubscriptionCommand : CommandBase
{
    private readonly ILogger _logger;
    private readonly SubscriptionSettings _subscriptionSettings;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly UserProvider _userProvider;
    private readonly ChannelProvider _channelProvider;
    private readonly NotificationService _notificationService;
    private readonly SeriesProvider _seriesRepository;
    private readonly IDialogService _dialogService;
    private readonly ISelectionDialogService _selectionDialogService;
    private readonly ISubscriptionDialogService _subscriptionDialogService;

    // 前回選択の購読グループを記憶する
    // 設定項目として永続化するには馴染まないのでアプリ起動中のみ保持
    static SubscriptionGroupId? _lastSelectedSubscriptionGroupId;

    public AddSubscriptionCommand(
        ILogger logger,
        SubscriptionSettings subscriptionSettings,
        SubscriptionManager subscriptionManager,
        UserProvider userProvider,
        ChannelProvider channelProvider,
        NotificationService notificationService,
        SeriesProvider seriesRepository,
        IDialogService dialogService,
        ISelectionDialogService selectionDialogService,
        ISubscriptionDialogService subscriptionDialogService
        )
    {
        _logger = logger;
        _subscriptionSettings = subscriptionSettings;
        _subscriptionManager = subscriptionManager;
        _userProvider = userProvider;
        _channelProvider = channelProvider;
        _notificationService = notificationService;
        _seriesRepository = seriesRepository;
        _dialogService = dialogService;
        _selectionDialogService = selectionDialogService;
        _subscriptionDialogService = subscriptionDialogService;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is IVideoContentProvider
            || parameter is IMylist
            || parameter is IChannel
            || parameter is IUser
            || parameter is ISeries
            ;
    }

    protected override async void Execute(object parameter)
    {
        try
        {
            // 購読登録用にデータを正規化
            (string id, SubscriptionSourceType sourceType, string? label) result = parameter switch
            {
                SubscVideoListItemViewModel subscItemVM => subscItemVM.GetSubscriptionParameter(),
                IVideoContentProvider videoContent => videoContent.ProviderType switch
                {
                    OwnerType.User => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.User, default(string?)),
                    OwnerType.Channel => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.Channel, default(string?)),
                    _ => throw new NotSupportedException()
                },
                IMylist mylist => (id: mylist.PlaylistId.Id, sourceType: SubscriptionSourceType.Mylist, mylist.Name),
                IUser user => (id: user.UserId, sourceType: SubscriptionSourceType.User, user.Nickname),
                ISeries series => (id: series.Id.ToString(), sourceType: SubscriptionSourceType.Series, series.Title),
                _ => throw new NotSupportedException(),
            };

            // 購読アイテム名の解決
            if (string.IsNullOrEmpty(result.label))
            {
                result.label = result.sourceType switch
                {
                    //SubscriptionSourceType.Mylist => await ResolveMylistName(result.id),
                    SubscriptionSourceType.User => await ResolveUserName(result.id),
                    SubscriptionSourceType.Channel => await ResolveChannelName(result.id),
                    SubscriptionSourceType.Series => await ResolveSeriesName(result.id),
                    SubscriptionSourceType.SearchWithKeyword => result.id,
                    SubscriptionSourceType.SearchWithTag => result.id,
                    _ => throw new NotSupportedException(),
                };
            }

            // 登録先となる購読グループの取得
            bool alreadyAdded = _subscriptionManager.TryGetSubscriptionGroup(result.sourceType, result.id, out Subscription? alreadySource, out SubscriptionGroup? defaultGroup);

            var groups = new[] { _subscriptionManager.DefaultSubscriptionGroup }
                .Concat(_subscriptionManager.GetSubscriptionGroups())                
                .ToList();

            // デフォルト指定するグループの解決
            if (!alreadyAdded && _lastSelectedSubscriptionGroupId.HasValue)
            {
                defaultGroup = groups.FirstOrDefault(x => x.GroupId == _lastSelectedSubscriptionGroupId.Value);
            }

            defaultGroup ??= _subscriptionManager.DefaultSubscriptionGroup;

            // 購読グループの選択
            var selectedGroup = await _selectionDialogService.ShowSingleSelectDialogAsync(
                groups,
                defaultGroup,
                displayMemberPath: nameof(SubscriptionGroup.Name),
                dialogTitle: "SubscGroup_SelectGroup".Translate(),
                primaryButtonText: "Select".Translate(),
                secondaryButtonText: "CreateNew".Translate(),
                secondaryButtonAction: async () => 
                {
                    if (await _subscriptionDialogService.ShowSubscriptionGroupCreateDialogAsync(
                        "",
                        isAutoUpdateDefault: _subscriptionSettings.Default_IsAutoUpdate,
                        isAddToQueueeDefault: _subscriptionSettings.Default_IsAddToQueue,
                        isToastNotificationDefault: _subscriptionSettings.Default_IsToastNotification,
                        isShowMenuItemDefault: _subscriptionSettings.Default_IsShowMenuItem
                        )
                        is { } result && result.IsSuccess is false)
                    {
                        return default!;
                    }

                    _subscriptionSettings.Default_IsAutoUpdate = result.IsAutoUpdate;
                    _subscriptionSettings.Default_IsAddToQueue = result.IsAddToQueue;
                    _subscriptionSettings.Default_IsToastNotification= result.IsToastNotification;
                    _subscriptionSettings.Default_IsShowMenuItem = result.IsShowMenuItem;
                    
                    var subscGroup = _subscriptionManager.CreateSubscriptionGroup(result.Title);
                    var props = _subscriptionManager.GetSubscriptionGroupProps(subscGroup.GroupId);
                    props.IsAutoUpdateEnabled = result.IsAutoUpdate;
                    props.IsAddToQueueWhenUpdated = result.IsAddToQueue;
                    props.IsToastNotificationEnabled = result.IsToastNotification;
                    props.IsShowInAppMenu = result.IsShowMenuItem;
                    _subscriptionManager.SetSubcriptionGroupProps(props);
                    return subscGroup;
                }
                );

            // 選択されなかった場合は何もせず終了
            if (selectedGroup == null) { return; }

            // デフォルトのグループは 内部的に Subscription.Group == null として扱っている
            if (selectedGroup.GroupId == SubscriptionGroupId.DefaultGroupId)
            {
                selectedGroup = null;
            }

            // 次回の初期選択グループの記憶
            _lastSelectedSubscriptionGroupId = selectedGroup?.GroupId;

            // 購読を登録
            if (!alreadyAdded)
            {
                var subscription = _subscriptionManager.AddSubscription(result.sourceType, result.id, result.label!, selectedGroup);
                if (subscription != null)
                {
                    Debug.WriteLine($"subscription added: {subscription.SubscriptionId} {subscription.Label} {subscription.SubscriptionId}");
                    _notificationService.ShowLiteInAppNotification_Success("Notification_SuccessAddSubscriptionSourceWithLabel".Translate(subscription.Label));

                    //Analytics.TrackEvent("Subscription_Added", new Dictionary<string, string>
                    //    {
                    //        { "SourceType", result.sourceType.ToString() }
                    //    });
                }
            }
            else if (alreadySource.Group != selectedGroup)
            {
                alreadySource.Group = selectedGroup!;
                _subscriptionManager.UpdateSubscription(alreadySource);

                Debug.WriteLine($"subscription updated: {alreadySource.SubscriptionId} {alreadySource.Label} {alreadySource.SubscriptionId}");
                _notificationService.ShowLiteInAppNotification_Success("Notification_SuccessAddSubscriptionSourceWithLabel".Translate(alreadySource.Label));
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, "購読追加時にエラーが発生しました。");
        }

    }

    async Task<string> ResolveUserName(string id)
    {
        var user = await _userProvider.GetUserInfoAsync(id);
        return user.ScreenName;
    }

    async Task<string> ResolveChannelName(string id)
    {
        var info = await _channelProvider.GetChannelInfo(id);
        return info.Name;
    }

    async Task<string> ResolveSeriesName(string id)
    {
        var seriesDetails = await _seriesRepository.GetSeriesVideosAsync(id);
        return seriesDetails.Data.Detail.Title;
    }
}

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
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Video;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Subscriptions;

public sealed class AddSubscriptionCommand : CommandBase
{
    private readonly ILogger _logger;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly UserProvider _userProvider;
    private readonly ChannelProvider _channelProvider;
    private readonly NotificationService _notificationService;
    private readonly SeriesProvider _seriesRepository;
    private readonly IDialogService _dialogService;
    private readonly ISelectionDialogService _selectionDialogService;

    // 前回選択の購読グループを記憶する
    // 設定項目として永続化するには馴染まないのでアプリ起動中のみ保持
    static SubscriptionGroupId? _lastSelectedSubscriptionGroupId;

    public AddSubscriptionCommand(
        ILogger logger,
        SubscriptionManager subscriptionManager,
        UserProvider userProvider,
        ChannelProvider channelProvider,
        NotificationService notificationService,
        SeriesProvider seriesRepository,
        IDialogService dialogService,
        ISelectionDialogService selectionDialogService
        )
    {
        _logger = logger;
        _subscriptionManager = subscriptionManager;
        _userProvider = userProvider;
        _channelProvider = channelProvider;
        _notificationService = notificationService;
        _seriesRepository = seriesRepository;
        _dialogService = dialogService;
        _selectionDialogService = selectionDialogService;
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
                .Concat(_subscriptionManager.GetSubscGroups())                
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
                    string? text = await _dialogService.GetTextAsync(
                        title: "SubscGroup_CreateGroup".Translate(),
                        placeholder: "",
                        defaultText: "",
                        validater: (s) => !string.IsNullOrWhiteSpace(s)
                        );
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return default;
                    }

                    return _subscriptionManager.CreateSubscriptionGroup(text);
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
            _logger.LogError(e, "購読追加時にエラーが発生しました。");
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

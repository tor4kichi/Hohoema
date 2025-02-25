#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Niconico;
using Hohoema.Services.Playlist;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using ZLogger;

namespace Hohoema.ViewModels.Pages.Niconico.Mylist;

using MylistFollowContext = FollowContext<IMylist>;

public sealed partial class MylistPageViewModel 
    : VideoListingPageViewModelBase<VideoListItemControlViewModel>
    , IPinablePage
    , ITitleUpdatablePage
	{
    HohoemaPin IPinablePage.GetPin()
    {
        return new HohoemaPin()
        {
            Label = Mylist.Value.Name,
            PageType = HohoemaPageType.Mylist,
            Parameter = $"id={Mylist.Value.MylistId}"
        };
    }

    IObservable<string> ITitleUpdatablePage.GetTitleObservable()
    {
        return Mylist.Select(x => x?.Name);
    }


    public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }

    public MylistPlaylistSortOption[] SortItems => MylistPlaylist.SortOptions;

    public ReactiveProperty<MylistPlaylistSortOption> SelectedSortOptionItem { get; }

    private readonly IMessenger _messenger;
    private readonly MylistFollowProvider _mylistFollowProvider;
    private readonly MylistResolver _mylistRepository;
    private readonly MylistUserSelectedSortRepository _mylistUserSelectedSortRepository;

    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public NiconicoSession NiconicoSession { get; }
    public MylistProvider MylistProvider { get; }
    public UserProvider UserProvider { get; }
    public LoginUserMylistProvider LoginUserMylistProvider { get; }
    public LoginUserOwnedMylistManager UserMylistManager { get; }
    public LocalMylistManager LocalMylistManager { get; }
    public SubscriptionManager SubscriptionManager { get; }
    public IMylistGroupDialogService DialogService { get; }
    public AddSubscriptionCommand AddSubscriptionCommand { get; }
    public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
    public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public ReactiveProperty<MylistPlaylist?> Mylist { get; private set; }

    public int MaxItemsCount { get; private set; }

    public string OwnerUserId { get; private set; }

    private bool _IsUserOwnerdMylist;
    public bool IsUserOwnerdMylist
    {
        get { return _IsUserOwnerdMylist; }
        set { SetProperty(ref _IsUserOwnerdMylist, value); }
    }

    private bool _IsLoginUserDeflist;
    public bool IsLoginUserDeflist
    {
        get { return _IsLoginUserDeflist; }
        set { SetProperty(ref _IsLoginUserDeflist, value); }
    }

    private bool _IsWatchAfterLocalMylist;
    public bool IsWatchAfterLocalMylist
    {
        get { return _IsWatchAfterLocalMylist; }
        set { SetProperty(ref _IsWatchAfterLocalMylist, value); }
    }

    private bool _IsLocalMylist;
    public bool IsLocalMylist
    {
        get { return _IsLocalMylist; }
        set { SetProperty(ref _IsLocalMylist, value); }
    }

    private string _UserName;
    public string UserName
    {
        get { return _UserName; }
        set { SetProperty(ref _UserName, value); }
    }

    public int DeflistRegistrationCapacity { get; private set; }
    public int DeflistRegistrationCount { get; private set; }
    public int MylistRegistrationCapacity { get; private set; }
    public int MylistRegistrationCount { get; private set; }


    private bool _IsMylistNotFound;
    public bool IsMylistNotFound
    {
        get { return _IsMylistNotFound; }
        set { SetProperty(ref _IsMylistNotFound, value); }
    }

    // Follow
    private MylistFollowContext _FollowContext = MylistFollowContext.Default;
    public MylistFollowContext FollowContext
    {
        get => _FollowContext;
        set => SetProperty(ref _FollowContext, value);
    }

    [ObservableProperty]
    public partial bool NowLoading { get; set; }

    public MylistPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        ApplicationLayoutManager applicationLayoutManager,        
        NiconicoSession niconicoSession,
        MylistProvider mylistProvider,
        MylistFollowProvider mylistFollowProvider,
        UserProvider userProvider,
        LoginUserMylistProvider loginUserMylistProvider,
        LoginUserOwnedMylistManager userMylistManager,
        LocalMylistManager localMylistManager,
        MylistResolver mylistRepository,
        SubscriptionManager subscriptionManager,
        MylistUserSelectedSortRepository mylistUserSelectedSortRepository,
        IMylistGroupDialogService dialogService,
        AddSubscriptionCommand addSubscriptionCommand,
        SelectionModeToggleCommand selectionModeToggleCommand,
        PlaylistPlayAllCommand playlistPlayAllCommand,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand
        )
        : base(messenger, loggerFactory.CreateLogger<MylistPageViewModel>(), disposeItemVM: false)
    {
        _messenger = messenger;
        ApplicationLayoutManager = applicationLayoutManager;
        NiconicoSession = niconicoSession;
        MylistProvider = mylistProvider;
        _mylistFollowProvider = mylistFollowProvider;
        UserProvider = userProvider;
        LoginUserMylistProvider = loginUserMylistProvider;
        UserMylistManager = userMylistManager;
        LocalMylistManager = localMylistManager;
        _mylistRepository = mylistRepository;
        SubscriptionManager = subscriptionManager;
        _mylistUserSelectedSortRepository = mylistUserSelectedSortRepository;
        DialogService = dialogService;
        AddSubscriptionCommand = addSubscriptionCommand;
        SelectionModeToggleCommand = selectionModeToggleCommand;
        PlaylistPlayAllCommand = playlistPlayAllCommand;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        Mylist = new ReactiveProperty<MylistPlaylist>();

        SelectedSortOptionItem = new ReactiveProperty<MylistPlaylistSortOption>(mode: ReactivePropertyMode.DistinctUntilChanged);

        CurrentPlaylistToken = Observable.CombineLatest(
            Mylist,
            SelectedSortOptionItem,
            (x, y) => new PlaylistToken(x, y)
            )
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_CompositeDisposable);


        /*
        IsFavoriteMylist = new ReactiveProperty<bool>(mode: ReactivePropertyMode.DistinctUntilChanged)
            .AddTo(_CompositeDisposable);
        CanChangeFavoriteMylistState = new ReactiveProperty<bool>()
            .AddTo(_CompositeDisposable);


        IsFavoriteMylist
            .Where(x => PlayableList.Value.Id != null)
            .Subscribe(async x =>
            {
                if (PlayableList.Value.Origin != PlaylistOrigin.OtherUser) { return; }

                if (_NowProcessFavorite) { return; }

                _NowProcessFavorite = true;

                CanChangeFavoriteMylistState.Value = false;
                if (x)
                {
                    if (await FavoriteMylist())
                    {
                        Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り登録しました.");
                    }
                    else
                    {
                        // お気に入り登録に失敗した場合は状態を差し戻し
                        Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り登録に失敗");
                        IsFavoriteMylist.Value = false;
                    }
                }
                else
                {
                    if (await UnfavoriteMylist())
                    {
                        Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り解除しました.");
                    }
                    else
                    {
                        // お気に入り解除に失敗した場合は状態を差し戻し
                        Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り解除に失敗");
                        IsFavoriteMylist.Value = true;
                    }
                }

                CanChangeFavoriteMylistState.Value =
                    IsFavoriteMylist.Value == true
                    || FollowManager.CanMoreAddFollow(FollowItemType.Mylist);


                _NowProcessFavorite = false;
            })
            .AddTo(_CompositeDisposable);


        UnregistrationMylistCommand = SelectedItems.ObserveProperty(x => x.Count)
            .Where(_ => IsUserOwnerdMylist)
            .Select(x => x > 0)
            .ToReactiveCommand(false);

        UnregistrationMylistCommand.Subscribe(async _ =>
        {
            if (PlayableList.Value.Origin == PlaylistOrigin.Local)
            {
                var localMylist = PlayableList.Value as LegacyLocalMylist;
                var items = SelectedItems.ToArray();

                foreach (var item in items)
                {
                    localMylist.Remove(item.PlaylistItem);
                    IncrementalLoadingItems.Remove(item);
                }
            }
            else if (PlayableList.Value.Origin == PlaylistOrigin.LoginUser)
            {
                var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(PlayableList.Value.Id);

                var items = SelectedItems.ToArray();


                var action = AsyncInfo.Run<uint>(async (cancelToken, progress) =>
                {
                    uint progressCount = 0;
                    int successCount = 0;
                    int failedCount = 0;

                    Debug.WriteLine($"マイリストに追加解除を開始...");
                    foreach (var video in items)
                    {
                        var unregistrationResult = await mylistGroup.Unregistration(
                            video.RawVideoId
                            , withRefresh: false );

                        if (unregistrationResult == ContentManageResult.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                        }

                        progressCount++;
                        progress.Report(progressCount);

                        Debug.WriteLine($"{video.Label}[{video.RawVideoId}]:{unregistrationResult.ToString()}");
                    }

                    // 登録解除結果を得るためリフレッシュ
                    await mylistGroup.Refresh();


                    // ユーザーに結果を通知
                    var titleText = $"「{mylistGroup.Label}」から {successCount}件 の動画が登録解除されました";
                    var toastService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NotificationService>();
                    var resultText = $"";
                    if (failedCount > 0)
                    {
                        resultText += $"\n登録解除に失敗した {failedCount}件 は選択されたままです";
                    }
                    toastService.ShowToast(titleText, resultText);

                    // 登録解除に失敗したアイテムだけを残すように
                    // マイリストから除外された動画を選択アイテムリストから削除
                    foreach (var item in SelectedItems.ToArray())
                    {
                        if (false == mylistGroup.CheckRegistratedVideoId(item.RawVideoId))
                        {
                            SelectedItems.Remove(item);
                            IncrementalLoadingItems.Remove(item);
                        }
                    }

                    Debug.WriteLine($"マイリストに追加解除完了---------------");
                });

                await PageManager.StartNoUIWork("マイリストに追加解除", items.Length, () => action);

            }


        });


        */
    }


    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        NowLoading = true;
        try
        {
            IsMylistNotFound = false;
            MylistId? maybeMylistId = null;

            if (parameters.TryGetValue<MylistId>("id", out var justMylistId))
            {
                maybeMylistId = justMylistId;
            }
            else if (parameters.TryGetValue<string>("id", out var idString))
            {
                maybeMylistId = idString;
            }
            else if (parameters.TryGetValue<uint>("id", out var idInt))
            {
                maybeMylistId = idInt;
            }

            if (maybeMylistId == null)
            {

            }

            var mylistId = maybeMylistId.Value;
            var mylist = await _mylistRepository.GetMylistAsync(mylistId);

            if (mylist == null)
            {
                return;
            }

            Mylist.Value = mylist;

            IsUserOwnerdMylist = _mylistRepository.IsLoginUserMylistId(mylist.MylistId);
            IsLoginUserDeflist = mylist.MylistId.IsWatchAfterMylist;
            IsWatchAfterLocalMylist = false;
            IsLocalMylist = false;

            if (mylist is LoginUserMylistPlaylist loginMylist)
            {
                Observable.FromEventPattern<MylistItemAddedEventArgs>(
                h => loginMylist.MylistItemAdded += h,
                h => loginMylist.MylistItemAdded -= h
                )
                .Subscribe(e =>
                {
                    var args = e.EventArgs;
                    if (args.MylistId == Mylist.Value.MylistId)
                    {
                        RefreshCommand.Execute(null);
                    }
                })
                .AddTo(_navigationDisposables);

                Observable.FromEventPattern<MylistItemRemovedEventArgs>(
                    h => loginMylist.MylistItemRemoved += h,
                    h => loginMylist.MylistItemRemoved -= h
                    )
                    .Subscribe(e =>
                    {
                        if (ItemsView == null) { return; }
                        var args = e.EventArgs;
                        if (args.MylistId == Mylist.Value.MylistId)
                        {
                            foreach (var removed in args.SuccessedItems)
                            {
                                var removedItem = ItemsView.Cast<VideoItemViewModel>().FirstOrDefault(x => x.VideoId == removed.VideoId);
                                if (removedItem != null)
                                {
                                    ItemsView.Remove(removedItem);
                                }
                            }
                        }
                    })
                    .AddTo(_navigationDisposables);

                Observable.FromEventPattern<MylistItemMovedEventArgs>(
                    h => loginMylist.MylistMoved += h,
                    h => loginMylist.MylistMoved -= h
                    )
                    .Subscribe(e =>
                    {
                        if (ItemsView == null) { return; }
                        var args = e.EventArgs;
                        if (args.SourceMylistId == Mylist.Value.MylistId)
                        {
                            foreach (var id in args.SuccessedItems)
                            {
                                var removeTarget = ItemsView.Cast<VideoItemViewModel>().FirstOrDefault(x => x.VideoId == id);
                                ItemsView.Remove(removeTarget);
                            }
                        }
                    })
                    .AddTo(_navigationDisposables);
            }

            var lastSort = _mylistUserSelectedSortRepository.GetMylistSort(mylistId);
            if (!IsLoginUserDeflist)
            {
                SelectedSortOptionItem.Value = SortItems.First(x => x.SortKey == (lastSort.SortKey ?? Mylist.Value.DefaultSortKey) && x.SortOrder == (lastSort.SortOrder ?? Mylist.Value.DefaultSortOrder));
            }
            else
            {
                SelectedSortOptionItem.Value = SortItems.First(x => x.SortKey == (lastSort.SortKey ?? MylistSortKey.AddedAt) && x.SortOrder == (lastSort.SortOrder ?? MylistSortOrder.Desc));
            }

            try
            {
                if (NiconicoSession.IsLoggedIn && Mylist.Value != null)
                {
                    FollowContext = await MylistFollowContext.CreateAsync(_mylistFollowProvider, Mylist.Value);
                }
                else
                {
                    FollowContext = MylistFollowContext.Default;
                }
            }
            catch
            {
                FollowContext = MylistFollowContext.Default;
            }

            SelectedSortOptionItem
                .Where(x => x is not null)
                .Subscribe(x =>
                {
                    RefreshCommand.Execute(null);

                    _mylistUserSelectedSortRepository.SetMylistSort(Mylist.Value.MylistId, x.SortKey, x.SortOrder);
                })
                .AddTo(_navigationDisposables);

            EditMylistGroupCommand.NotifyCanExecuteChanged();
            DeleteMylistCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            NowLoading = false;
        }

        await base.OnNavigatedToAsync(parameters);
    }

    protected override void PostResetList()
    {
        MaxItemsCount = Mylist.Value.Count;

        base.PostResetList();
    }
    protected override (int PageSize, IIncrementalSource<VideoListItemControlViewModel> IncrementalSource) GenerateIncrementalSource()
    {
        var sortOption = SelectedSortOptionItem.Value;
        if (sortOption == null)
        {
            var lastSort = _mylistUserSelectedSortRepository.GetMylistSort(Mylist.Value.MylistId);
            if (!IsLoginUserDeflist)
            {
                sortOption = SelectedSortOptionItem.Value = SortItems.First(x => x.SortKey == (lastSort.SortKey ?? Mylist.Value.DefaultSortKey) && x.SortOrder == (lastSort.SortOrder ?? Mylist.Value.DefaultSortOrder));
            }
            else
            {
                sortOption = SelectedSortOptionItem.Value = SortItems.First(x => x.SortKey == (lastSort.SortKey ?? MylistSortKey.AddedAt) && x.SortOrder == (lastSort.SortOrder ?? MylistSortOrder.Desc));
            }
        }

        if (Mylist.Value is LoginUserMylistPlaylist loginUserMylist)
        {
            return (25, new LoginUserMylistIncrementalSource(loginUserMylist, sortOption, _logger));

        }
        else
        {
            return (25, new MylistIncrementalSource(Mylist.Value, sortOption, _logger));
        }
    }


    #region Commands


    private RelayCommand _OpenMylistOwnerCommand;
    public RelayCommand OpenMylistOwnerCommand
    {
        get
        {
            return _OpenMylistOwnerCommand
                ?? (_OpenMylistOwnerCommand = new RelayCommand(() =>
                {
                    _messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, Mylist.Value.UserId);
                }));
        }
    }


    public ReactiveCommand UnregistrationMylistCommand { get; private set; }
    public ReactiveCommand CopyMylistCommand { get; private set; }
    public ReactiveCommand MoveMylistCommand { get; private set; }

    private RelayCommand<IPlaylist> _EditMylistGroupCommand;
    public RelayCommand<IPlaylist> EditMylistGroupCommand
    {
        get
        {
            return _EditMylistGroupCommand
                ?? (_EditMylistGroupCommand = new RelayCommand<IPlaylist>(async playlist =>
                {
                    if (Mylist.Value is LoginUserMylistPlaylist mylist)
                    {
                        MylistGroupEditData data = new MylistGroupEditData()
                        {
                            Name = mylist.Name,
                            Description = mylist.Description,
                            IsPublic = mylist.IsPublic,
                            DefaultSortKey = mylist.DefaultSortKey,
                            DefaultSortOrder = mylist.DefaultSortOrder,
                        };

                        // 成功するかキャンセルが押されるまで繰り返す
                        while (true)
                        {
                            if (true == await DialogService.ShowEditMylistGroupDialogAsync(data))
                            {
                                var result = await LoginUserMylistProvider.UpdateMylist(mylist, data.Name, data.Description, data.IsPublic, data.DefaultSortKey, data.DefaultSortOrder);

                                if (result)
                                {
                                    Mylist.ForceNotify();

                                    //Microsoft.AppCenter.Analytics.Analytics.TrackEvent("Mylist_Edit");

                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    
                }
                , mylist => IsUserOwnerdMylist && !IsWatchAfterLocalMylist
                ));
        }
    }



    private RelayCommand<IPlaylist> _DeleteMylistCommand;
    public RelayCommand<IPlaylist> DeleteMylistCommand
    {
        get
        {
            return _DeleteMylistCommand
                ?? (_DeleteMylistCommand = new RelayCommand<IPlaylist>(async mylist =>
                {
                    // 確認ダイアログ
                    var mylistOrigin = mylist.GetOrigin();
                    var contentMessage = "ConfirmDeleteX_ImpossibleReDo".Translate(mylist.Name);

                    var dialog = new MessageDialog(contentMessage, $"ConfirmDeleteX".Translate(PlaylistItemsSourceOrigin.Mylist.Translate()));
                    dialog.Commands.Add(new UICommand("Delete".Translate(), async (i) =>
                    {
                        if (mylistOrigin == PlaylistItemsSourceOrigin.Local)
                        {
                            LocalMylistManager.RemovePlaylist(mylist as LocalPlaylist);
                        }
                        else if (mylistOrigin == PlaylistItemsSourceOrigin.Mylist)
                        {
                            await UserMylistManager.RemoveMylist(mylist.PlaylistId.Id);
                        }

                        await _messenger.OpenPageWithIdAsync(HohoemaPageType.UserMylist, OwnerUserId);

                        //Microsoft.AppCenter.Analytics.Analytics.TrackEvent("Mylist_Removed");
                    }));

                    dialog.Commands.Add(new UICommand("Cancel".Translate()));
                    dialog.CancelCommandIndex = 1;
                    dialog.DefaultCommandIndex = 1;

                    await dialog.ShowAsync();
                }
                , mylist =>
                {
                    if (mylist is LocalPlaylist)
                    {
                        return !mylist.IsUniquePlaylist();
                    }
                    else if (mylist is LoginUserMylistPlaylist loginUserMylist)
                    {
                        return !loginUserMylist.MylistId.IsWatchAfterMylist;
                    }
                    else
                    {
                        return false;
                    }
                }
                ));
        }
    }



    private RelayCommand _RefreshCommand;
    public RelayCommand RefreshCommand
    {
        get
        {
            return _RefreshCommand
                ?? (_RefreshCommand = new RelayCommand(async () =>
                {
                    ResetList();
                }));
        }
    }



    #endregion


    private RelayCommand<IVideoContent> _PlayWithCurrentPlaylistCommand;
    public RelayCommand<IVideoContent> PlayWithCurrentPlaylistCommand
    {
        get
        {
            return _PlayWithCurrentPlaylistCommand
                ?? (_PlayWithCurrentPlaylistCommand = new RelayCommand<IVideoContent>((video) =>
                {
                    _messenger.Send(new VideoPlayRequestMessage() { VideoId = video.VideoId, PlaylistId = Mylist.Value.MylistId, PlaylistOrigin = Mylist.Value.GetOrigin() });
                }
                ));
        }
    }
}

public class MylistIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
{
    private readonly MylistPlaylist _mylist;
    private readonly MylistPlaylistSortOption _sortOption;
    private readonly ILogger _logger;

    public MylistIncrementalSource(
        MylistPlaylist mylist,
        MylistPlaylistSortOption sortOption,
        ILogger logger
        )
        : base()
    {
        _mylist = mylist;
        _sortOption = sortOption;
        _logger = logger;
    }

    async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mylist.GetItemsAsync(pageIndex, pageSize, _sortOption.SortKey, _sortOption.SortOrder);
            var start = pageIndex * pageSize;
            return result.Items
                .Select((x, i) => new VideoListItemControlViewModel(x.Video) { PlaylistItemToken = new(_mylist, _sortOption, new NvapiVideoItemWrapped(x.Video)) })
                .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }
        catch (Exception e)
        {
            _logger.ZLogErrorWithPayload(exception: e, (MylistId: _mylist.MylistId, SortOption: _sortOption), "Mylist items loading error");
            return Enumerable.Empty<VideoListItemControlViewModel>();
        }
    }
}

public class LoginUserMylistIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
{
    private readonly LoginUserMylistPlaylist _mylist;
    private readonly MylistPlaylistSortOption _sortOption;
    private readonly ILogger _logger;

    public LoginUserMylistIncrementalSource(
        LoginUserMylistPlaylist mylist,
        MylistPlaylistSortOption sortOption,
        ILogger logger
        )
        : base()
    {
        _mylist = mylist;
        _sortOption = sortOption;
        _logger = logger;
    }

    bool isEndReached;

    async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    {
        try
        {
            if (isEndReached)
            {
                return Enumerable.Empty<VideoListItemControlViewModel>();
            }

            var items = await _mylist.GetItemsAsync(pageIndex, pageSize, _sortOption.SortKey, _sortOption.SortOrder);
            isEndReached = items.NicoVideoItems.Count != pageSize;

            ct.ThrowIfCancellationRequested();

            var start = pageIndex * pageSize;
            return items.Items
                .Select((x, i) => new VideoListItemControlViewModel(x.Video) { PlaylistItemToken = new(_mylist, _sortOption, new NvapiVideoItemWrapped(x.Video)) })
                .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }
        catch (Exception e)
        {
            _logger.ZLogErrorWithPayload(exception: e, (MylistId: _mylist.MylistId, SortOption: _sortOption), "LoginUserMylist items loading failed");
            return Enumerable.Empty<VideoListItemControlViewModel>();
        }
    }
}

public class NvapiVideoItemWrapped : IVideoContent
{
    private readonly NvapiVideoItem _video;

    public NvapiVideoItemWrapped(NvapiVideoItem video)
    {
        _video = video;
    }

    public VideoId VideoId => _video.Id;

    public TimeSpan Length => TimeSpan.FromSeconds(_video.Duration);

    public string ThumbnailUrl => _video.Thumbnail.Url.OriginalString;

    public DateTime PostedAt => _video.RegisteredAt.DateTime;

    public string Title => _video.Title;

    public bool Equals(IVideoContent other)
    {
        return _video.Id == other.VideoId;
    }
}

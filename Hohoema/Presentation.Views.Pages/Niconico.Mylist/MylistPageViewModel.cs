using Hohoema.Dialogs;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using I18NPortable;
using NiconicoToolkit.Mylist;
using Microsoft.Toolkit.Mvvm.Input;
using Hohoema.Presentation.Navigations;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.Domain.LocalMylist;
using NiconicoToolkit.Video;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Mylist
{
    using MylistFollowContext = FollowContext<IMylist>;

    public class MylistPageViewModel : HohoemaPageViewModelBase, IPinablePage, ITitleUpdatablePage
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

        public MylistPageViewModel(
            ILoggerFactory loggerFactory,
            IMessenger messenger,
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
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
            Services.DialogService dialogService,
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
            )
        {
            _logger = loggerFactory.CreateLogger<MylistPageViewModel>();
            _messenger = messenger;
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
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
                        var toastService = Microsoft.Toolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NotificationService>();
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

        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }

        public MylistPlaylistSortOption[] SortItems => MylistPlaylist.SortOptions;

        public ReactiveProperty<MylistPlaylistSortOption> SelectedSortOptionItem { get; }

        private readonly ILogger<MylistPageViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly MylistFollowProvider _mylistFollowProvider;
        private readonly MylistResolver _mylistRepository;
        private readonly MylistUserSelectedSortRepository _mylistUserSelectedSortRepository;
        
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }


        public NiconicoSession NiconicoSession { get; }
        public MylistProvider MylistProvider { get; }
        public UserProvider UserProvider { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public LoginUserOwnedMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public DialogService DialogService { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public ReactiveProperty<MylistPlaylist> Mylist { get; private set; }

        private ICollection<VideoListItemControlViewModel> _mylistItems;
        public ICollection<VideoListItemControlViewModel> MylistItems
        {
            get { return _mylistItems; }
            private set { SetProperty(ref _mylistItems, value); }
        }

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



        #region Commands


        private RelayCommand _OpenMylistOwnerCommand;
        public RelayCommand OpenMylistOwnerCommand
        {
            get
            {
                return _OpenMylistOwnerCommand
                    ?? (_OpenMylistOwnerCommand = new RelayCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.UserInfo, Mylist.Value.UserId);
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


                            PageManager.OpenPage(HohoemaPageType.UserMylist, OwnerUserId);

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
                        if (Mylist.Value != null)
                        {
                            MylistItems = CreateItemsSource(Mylist.Value);
                        }
                    }));
            }
        }



        #endregion


        /*

        private async Task<bool> FavoriteMylist()
		{
			if (PlayableList.Value == null) { return false; }
            if (PlayableList.Value.Origin != PlaylistOrigin.OtherUser) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.AddFollow(FollowItemType.Mylist, PlayableList.Value.Id, PlayableList.Value.Label);

			return result == ContentManageResult.Success || result == ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteMylist()
		{
			if (PlayableList.Value == null) { return false; }
            if (PlayableList.Value.Origin != PlaylistOrigin.OtherUser) { return false; }

            var favManager = HohoemaApp.FollowManager;
			var result = await favManager.RemoveFollow(FollowItemType.Mylist, PlayableList.Value.Id);

			return result == ContentManageResult.Success;

		}

    */

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await base.OnNavigatedToAsync(parameters);

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
                        var args = e.EventArgs;
                        if (args.MylistId == Mylist.Value.MylistId)
                        {
                            foreach (var removed in args.SuccessedItems)
                            {
                                var removedItem = MylistItems.FirstOrDefault(x => x.VideoId == removed.VideoId);
                                if (removedItem != null)
                                {
                                    MylistItems.Remove(removedItem);
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
                    var args = e.EventArgs;
                    if (args.SourceMylistId == Mylist.Value.MylistId)
                    {
                        foreach (var id in args.SuccessedItems)
                        {
                            var removeTarget = MylistItems.FirstOrDefault(x => x.VideoId == id);
                            MylistItems.Remove(removeTarget);
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



            MylistItems = CreateItemsSource(mylist);
            MaxItemsCount = Mylist.Value.Count;

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
       

        private ICollection<VideoListItemControlViewModel> CreateItemsSource(MylistPlaylist mylist)
        {
            var sortOption = SelectedSortOptionItem.Value;
            if (sortOption == null) 
            {
                var lastSort = _mylistUserSelectedSortRepository.GetMylistSort(Mylist.Value.MylistId);
                if (!IsLoginUserDeflist)
                {
                    SelectedSortOptionItem.Value = SortItems.First(x => x.SortKey == (lastSort.SortKey ?? Mylist.Value.DefaultSortKey) && x.SortOrder == (lastSort.SortOrder ?? Mylist.Value.DefaultSortOrder));
                }
                else
                {
                    SelectedSortOptionItem.Value = SortItems.First(x => x.SortKey == (lastSort.SortKey ?? MylistSortKey.AddedAt) && x.SortOrder == (lastSort.SortOrder ?? MylistSortOrder.Desc));
                }
            }

            if (mylist is LoginUserMylistPlaylist loginUserMylist)
            {
                return new HohoemaListingPageViewModelBase<VideoListItemControlViewModel>.HohoemaIncrementalLoadingCollection(
                    new LoginUserMylistIncrementalSource(loginUserMylist, sortOption, _logger)
                    );
            }
            else
            {
                return new HohoemaListingPageViewModelBase<VideoListItemControlViewModel>.HohoemaIncrementalLoadingCollection(
                    new MylistIncrementalSource(mylist, sortOption, _logger)
                    );
            }
        }

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
                return result.Items.Select((x, i) => new VideoListItemControlViewModel(x.Video) { PlaylistItemToken = new(_mylist, _sortOption, new NvapiVideoItemWrapped(x.Video)) });
            }
            catch (Exception e)
            {
                _logger.ZLogErrorWithPayload(exception:e, (MylistId: _mylist.MylistId, SortOption: _sortOption), "Mylist items loading error");
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
                return items.Items.Select((x, i) => new VideoListItemControlViewModel(x.Video) { PlaylistItemToken = new(_mylist, _sortOption, new NvapiVideoItemWrapped(x.Video)) });
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
}

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
using Hohoema.Presentation.ViewModels.Niconico.Mylist;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using I18NPortable;
using NiconicoToolkit.Mylist;
using Prism.Commands;
using Prism.Navigation;
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
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Mylist
{
    using MylistFollowContext = FollowContext<IMylist>;

    public class MylistPageViewModel : HohoemaPageViewModelBase, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            NiconicoSession niconicoSession,
            MylistProvider mylistProvider,
            MylistFollowProvider mylistFollowProvider,
            UserProvider userProvider,
            LoginUserMylistProvider loginUserMylistProvider,
            LoginUserOwnedMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            MylistRepository mylistRepository,
            HohoemaPlaylist hohoemaPlaylist,
            SubscriptionManager subscriptionManager,
            MylistUserSelectedSortRepository mylistUserSelectedSortRepository,
            Services.DialogService dialogService,
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
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
            HohoemaPlaylist = hohoemaPlaylist;
            SubscriptionManager = subscriptionManager;
            _mylistUserSelectedSortRepository = mylistUserSelectedSortRepository;
            DialogService = dialogService;
            AddSubscriptionCommand = addSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            Mylist = new ReactiveProperty<MylistPlaylist>();

            SelectedSortItem = new ReactiveProperty<MylistSortViewModel>(mode: ReactivePropertyMode.DistinctUntilChanged);

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
                        var toastService = App.Current.Container.Resolve<NotificationService>();
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

        static public MylistSortViewModel[] SortItems { get; } = new MylistSortViewModel[]
        {
            new MylistSortViewModel() { Key = MylistSortKey.AddedAt, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.AddedAt, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.Title, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.Title, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.MylistComment, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.MylistComment, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.RegisteredAt, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.RegisteredAt, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.ViewCount, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.ViewCount, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.LastCommentTime, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.LastCommentTime, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.CommentCount, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.CommentCount, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.MylistCount, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.MylistCount, Order = MylistSortOrder.Asc },
            new MylistSortViewModel() { Key = MylistSortKey.Duration, Order = MylistSortOrder.Desc },
            new MylistSortViewModel() { Key = MylistSortKey.Duration, Order = MylistSortOrder.Asc },
        };

        public ReactiveProperty<MylistSortViewModel> SelectedSortItem { get; }

        private readonly MylistFollowProvider _mylistFollowProvider;
        private readonly MylistRepository _mylistRepository;
        private readonly MylistUserSelectedSortRepository _mylistUserSelectedSortRepository;
        
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }


        public NiconicoSession NiconicoSession { get; }
        public MylistProvider MylistProvider { get; }
        public UserProvider UserProvider { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public LoginUserOwnedMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public DialogService DialogService { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }

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


        // Follow
        private MylistFollowContext _FollowContext = MylistFollowContext.Default;
        public MylistFollowContext FollowContext
        {
            get => _FollowContext;
            set => SetProperty(ref _FollowContext, value);
        }



        #region Commands


        private DelegateCommand _OpenMylistOwnerCommand;
        public DelegateCommand OpenMylistOwnerCommand
        {
            get
            {
                return _OpenMylistOwnerCommand
                    ?? (_OpenMylistOwnerCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.UserInfo, Mylist.Value.UserId);
                    }));
            }
        }


        public ReactiveCommand UnregistrationMylistCommand { get; private set; }
        public ReactiveCommand CopyMylistCommand { get; private set; }
        public ReactiveCommand MoveMylistCommand { get; private set; }





        private DelegateCommand<IPlaylist> _EditMylistGroupCommand;
        public DelegateCommand<IPlaylist> EditMylistGroupCommand
        {
            get
            {
                return _EditMylistGroupCommand
                    ?? (_EditMylistGroupCommand = new DelegateCommand<IPlaylist>(async playlist =>
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

                                        Microsoft.AppCenter.Analytics.Analytics.TrackEvent("Mylist_Edit");

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



        private DelegateCommand<IPlaylist> _DeleteMylistCommand;
        public DelegateCommand<IPlaylist> DeleteMylistCommand
        {
            get
            {
                return _DeleteMylistCommand
                    ?? (_DeleteMylistCommand = new DelegateCommand<IPlaylist>(async mylist =>
                    {
                        // 確認ダイアログ
                        var mylistOrigin = mylist.GetOrigin();
                        var contentMessage = "ConfirmDeleteX_ImpossibleReDo".Translate(mylist.Name);

                        var dialog = new MessageDialog(contentMessage, $"ConfirmDeleteX".Translate(PlaylistOrigin.Mylist.Translate()));
                        dialog.Commands.Add(new UICommand("Delete".Translate(), async (i) =>
                        {
                            if (mylistOrigin == PlaylistOrigin.Local)
                            {
                                LocalMylistManager.RemovePlaylist(mylist as LocalPlaylist);
                            }
                            else if (mylistOrigin == PlaylistOrigin.Mylist)
                            {
                                await UserMylistManager.RemoveMylist(mylist.Id);
                            }


                            PageManager.OpenPage(HohoemaPageType.UserMylist, OwnerUserId);

                            Microsoft.AppCenter.Analytics.Analytics.TrackEvent("Mylist_Removed");
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


        private DelegateCommand _PlayAllVideosFromHeadCommand;
        public DelegateCommand PlayAllVideosFromHeadCommand
        {
            get
            {
                return _PlayAllVideosFromHeadCommand
                    ?? (_PlayAllVideosFromHeadCommand = new DelegateCommand(() =>
                    {
                        var firstItem = MylistItems.FirstOrDefault();
                        if (firstItem != null)
                        {
                            HohoemaPlaylist.Play(firstItem, Mylist.Value);
                        }
                    }));
            }
        }

        private DelegateCommand _RefreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                return _RefreshCommand
                    ?? (_RefreshCommand = new DelegateCommand(async () =>
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

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            MylistId? maybeMylistId = null;

            if (parameters.TryGetValue<string>("id", out var idString))
            {
                maybeMylistId = idString;
            }
            else if (parameters.TryGetValue<uint>("id", out var idInt))
            {
                maybeMylistId = idInt;
            }
            if (parameters.TryGetValue<MylistId>("id", out var justMylistId))
            {
                maybeMylistId = justMylistId;
            }

            if (maybeMylistId == null) 
            {

            }

            var mylistId = maybeMylistId.Value;
            var mylist = await _mylistRepository.GetMylist(mylistId);

            if (mylist == null) { return; }

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
                        RefreshCommand.Execute();
                    }
                })
                .AddTo(_NavigatingCompositeDisposable);

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
                                var removedItem = MylistItems.FirstOrDefault(x => x.VideoId == removed);
                                if (removedItem != null)
                                {
                                    MylistItems.Remove(removedItem);
                                }
                            }
                        }
                    })
                    .AddTo(_NavigatingCompositeDisposable);
                
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
                .AddTo(_NavigatingCompositeDisposable);
            }

            var lastSort = _mylistUserSelectedSortRepository.GetMylistSort(mylistId);
            if (!IsLoginUserDeflist)
            {
                SelectedSortItem.Value = SortItems.First(x => x.Key == (lastSort.SortKey ?? Mylist.Value.DefaultSortKey) && x.Order == (lastSort.SortOrder ?? Mylist.Value.DefaultSortOrder));
            }
            else
            {
                SelectedSortItem.Value = SortItems.First(x => x.Key == (lastSort.SortKey ?? MylistSortKey.AddedAt) && x.Order == (lastSort.SortOrder ?? MylistSortOrder.Desc));
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

            SelectedSortItem.Subscribe(x =>
            {
                RefreshCommand.Execute();

                _mylistUserSelectedSortRepository.SetMylistSort(Mylist.Value.MylistId, x.Key, x.Order);
            })
                .AddTo(_NavigatingCompositeDisposable);

            EditMylistGroupCommand.RaiseCanExecuteChanged();
            DeleteMylistCommand.RaiseCanExecuteChanged();

            
        }
       

        private ICollection<VideoListItemControlViewModel> CreateItemsSource(MylistPlaylist mylist)
        {
            var sortItem = SelectedSortItem.Value;
            if (sortItem == null) { return null; }

            if (mylist is LoginUserMylistPlaylist loginUserMylist)
            {
                return new HohoemaListingPageViewModelBase<VideoListItemControlViewModel>.HohoemaIncrementalLoadingCollection(
                    new LoginUserMylistIncrementalSource(loginUserMylist, sortItem.Key, sortItem.Order)
                    );
            }
            else
            {
                return new HohoemaListingPageViewModelBase<VideoListItemControlViewModel>.HohoemaIncrementalLoadingCollection(
                    new MylistIncrementalSource(mylist, sortItem.Key, sortItem.Order)
                    );
            }
        }

        private DelegateCommand<IVideoContent> _PlayWithCurrentPlaylistCommand;
        public DelegateCommand<IVideoContent> PlayWithCurrentPlaylistCommand
        {
            get
            {
                return _PlayWithCurrentPlaylistCommand
                    ?? (_PlayWithCurrentPlaylistCommand = new DelegateCommand<IVideoContent>((video) =>
                    {
                        HohoemaPlaylist.PlayContinueWithPlaylist(video, Mylist.Value);
                    }
                    ));
            }
        }
    }
    
	public class MylistIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
	{
        private readonly MylistPlaylist _mylist;

        public MylistIncrementalSource(
            MylistPlaylist mylist,
            MylistSortKey defaultSortKey,
            MylistSortOrder defaultSortOrder
            )
            : base()
        {
            _mylist = mylist;
            DefaultSortKey = defaultSortKey;
            DefaultSortOrder = defaultSortOrder;
        }

        public MylistSortKey DefaultSortKey { get; }
        public MylistSortOrder DefaultSortOrder { get; }

        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mylist.GetItemsAsync(pageIndex, pageSize, DefaultSortKey, DefaultSortOrder);

                if (!result.IsSuccess || result.Items == null || !result.Items.Any())
                {
                    return Enumerable.Empty<VideoListItemControlViewModel>();
                }

                return result.Items.Select(x => new VideoListItemControlViewModel(x.Video));
            }
            catch (Exception e)
            {
                ErrorTrackingManager.TrackError(e);
                return Enumerable.Empty<VideoListItemControlViewModel>();
            }
        }

    }

    public class LoginUserMylistIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
    {
        private readonly LoginUserMylistPlaylist _mylist;
        
        public MylistSortKey DefaultSortKey { get; }
        public MylistSortOrder DefaultSortOrder { get; }

        public LoginUserMylistIncrementalSource(
            LoginUserMylistPlaylist mylist,
            MylistSortKey defaultSortKey,
            MylistSortOrder defaultSortOrder
            )
            : base()
        {
            _mylist = mylist;
            DefaultSortKey = defaultSortKey;
            DefaultSortOrder = defaultSortOrder;
        }

        bool isEndReached;

        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            if (isEndReached)
            {
                return Enumerable.Empty<VideoListItemControlViewModel>();
            }

            var items = await _mylist.GetLoginUserMylistItemsAsync(pageIndex, pageSize, DefaultSortKey, DefaultSortOrder);
            isEndReached = items.Count != pageSize;

            ct.ThrowIfCancellationRequested();

            return items.Select(x => new VideoListItemControlViewModel(x.MylistItem.Video));
        }
    }
}

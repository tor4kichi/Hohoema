using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Mntone.Nico2.Mylist;
using Reactive.Bindings;
using System.Reactive.Linq;
using NicoPlayerHohoema.Models.Helpers;
using System.Threading;
using Unity;
using Windows.UI;
using Windows.UI.Popups;
using NicoPlayerHohoema.Dialogs;
using System.Collections.Async;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using Prism.Navigation;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.Repository.Playlist;

namespace NicoPlayerHohoema.ViewModels
{
    public class MylistPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
	{

        public MylistPageViewModel(
            Services.PageManager pageManager,
            NiconicoSession niconicoSession,
            MylistProvider mylistProvider,
            UserProvider userProvider,
            FollowManager followManager,
            LoginUserMylistProvider loginUserMylistProvider,
            NGSettings ngSettings,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            MylistRepository mylistRepository,
            HohoemaPlaylist hohoemaPlaylist,
            SubscriptionManager subscriptionManager,
            Services.DialogService dialogService,
            NiconicoFollowToggleButtonService followToggleButtonService,
            PlaylistAggregateGetter playlistAggregate,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
            )
        {
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            MylistProvider = mylistProvider;
            UserProvider = userProvider;
            FollowManager = followManager;
            LoginUserMylistProvider = loginUserMylistProvider;
            NgSettings = ngSettings;
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
            _mylistRepository = mylistRepository;
            HohoemaPlaylist = hohoemaPlaylist;
            SubscriptionManager = subscriptionManager;
            DialogService = dialogService;
            FollowToggleButtonService = followToggleButtonService;
            _playlistAggregate = playlistAggregate;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
            Playlist = new ReactiveProperty<Interfaces.IPlaylist>();
            PlaylistOrigin = new ReactiveProperty<PlaylistOrigin>();

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


        private readonly MylistRepository _mylistRepository;
        private readonly PlaylistAggregateGetter _playlistAggregate;

        public PageManager PageManager { get; }


        public NiconicoSession NiconicoSession { get; }
        public MylistProvider MylistProvider { get; }
        public UserProvider UserProvider { get; }
        public FollowManager FollowManager { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public NGSettings NgSettings { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public Services.DialogService DialogService { get; }
        public NiconicoFollowToggleButtonService FollowToggleButtonService { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }

       
        public ReactiveProperty<Interfaces.IPlaylist> Playlist { get; private set; }

        public ReactiveProperty<PlaylistOrigin> PlaylistOrigin { get; }


        private ICollection<IVideoContent> _mylistItems;
        public ICollection<IVideoContent> MylistItems
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

        public ReactiveProperty<bool> IsFavoriteMylist { get; private set; }
        public ReactiveProperty<bool> CanChangeFavoriteMylistState { get; private set; }

        public int DeflistRegistrationCapacity { get; private set; }
        public int DeflistRegistrationCount { get; private set; }
        public int MylistRegistrationCapacity { get; private set; }
        public int MylistRegistrationCount { get; private set; }


        public Database.Bookmark MylistBookmark { get; private set; }

        #region Commands


        private DelegateCommand _OpenMylistOwnerCommand;
        public DelegateCommand OpenMylistOwnerCommand
        {
            get
            {
                return _OpenMylistOwnerCommand
                    ?? (_OpenMylistOwnerCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.UserInfo, OwnerUserId);
                    }));
            }
        }


        public ReactiveCommand UnregistrationMylistCommand { get; private set; }
        public ReactiveCommand CopyMylistCommand { get; private set; }
        public ReactiveCommand MoveMylistCommand { get; private set; }





        private DelegateCommand<Interfaces.IPlaylist> _EditMylistGroupCommand;
        public DelegateCommand<Interfaces.IPlaylist> EditMylistGroupCommand
        {
            get
            {
                return _EditMylistGroupCommand
                    ?? (_EditMylistGroupCommand = new DelegateCommand<Interfaces.IPlaylist>(async playlist =>
                    {
                        if (playlist is LocalPlaylist localMylist)
                        {
                            var resultText = await DialogService.GetTextAsync("プレイリスト名を変更",
                                localMylist.Label,
                                localMylist.Label,
                                (tempName) => !string.IsNullOrWhiteSpace(tempName)
                                );

                            if (!string.IsNullOrWhiteSpace(resultText))
                            {
                                localMylist.Label = resultText;
                                PageManager.PageTitle = resultText;
                            }
                        }

                        if (playlist is LoginUserMylistPlaylist mylist)
                        {
                            MylistGroupEditData data = new MylistGroupEditData()
                            {
                                Name = mylist.Label,
                                Description = mylist.Description,
                                IsPublic = mylist.IsPublic,
                                IconType = mylist.IconType,
                            };

                            // 成功するかキャンセルが押されるまで繰り返す
                            while (true)
                            {
                                if (true == await DialogService.ShowEditMylistGroupDialogAsync(data))
                                {
                                    var result = await LoginUserMylistProvider.UpdateMylist(mylist.Id, data);

                                    if (result == Mntone.Nico2.ContentManageResult.Success)
                                    {
                                        mylist.Label = data.Name;
                                        mylist.IsPublic = data.IsPublic;
                                        mylist.DefaultSort = data.MylistDefaultSort;
                                        mylist.IconType = data.IconType;
                                        mylist.Description = data.Description;

                                        PageManager.PageTitle = data.Name;

                                        // TODO: IsPublicなどの情報を表示

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



        private DelegateCommand<Interfaces.IPlaylist> _DeleteMylistCommand;
        public DelegateCommand<Interfaces.IPlaylist> DeleteMylistCommand
        {
            get
            {
                return _DeleteMylistCommand
                    ?? (_DeleteMylistCommand = new DelegateCommand<Interfaces.IPlaylist>(async mylist =>
                    {
                        // 確認ダイアログ
                        var mylistOrigin = mylist.GetOrigin();
                        var originText = mylistOrigin == Interfaces.PlaylistOrigin.Local ? "ローカルマイリスト" : "マイリスト";
                        var contentMessage = $"{mylist.Label} を削除してもよろしいですか？（変更は元に戻せません）";

                        var dialog = new MessageDialog(contentMessage, $"{originText}削除の確認");
                        dialog.Commands.Add(new UICommand("削除", async (i) =>
                        {
                            if (mylistOrigin == Interfaces.PlaylistOrigin.Local)
                            {
                                LocalMylistManager.RemovePlaylist(mylist as LocalPlaylist);
                            }
                            else if (mylistOrigin == Interfaces.PlaylistOrigin.Mylist)
                            {
                                await UserMylistManager.RemoveMylist(mylist.Id);
                            }


                            PageManager.OpenPage(HohoemaPageType.UserMylist, OwnerUserId);
                        }));

                        dialog.Commands.Add(new UICommand("キャンセル"));
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
                            return !loginUserMylist.IsDefaultMylist();
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
                            HohoemaPlaylist.Play(firstItem, Playlist.Value);
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
                        MylistItems = await CreateItemsSourceAsync(Playlist.Value);
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
            string mylistId = null;

            if (parameters.TryGetValue<string>("id", out var idString))
            {
                mylistId = idString;
            }
            else if (parameters.TryGetValue<int>("id", out var idInt))
            {
                mylistId = idInt.ToString();
            }

            var playlist = await _playlistAggregate.FindPlaylistAsync(mylistId);

            if (playlist == null) { return; }

            Playlist.Value = playlist;
            PlaylistOrigin.Value = playlist.GetOrigin();

            if (playlist is LocalPlaylist localPlaylist || playlist is PlaylistObservableCollection)
            {
                IsUserOwnerdMylist = true;
                IsLoginUserDeflist = false;
                IsWatchAfterLocalMylist = playlist.IsWatchAfterPlaylist();
                IsLocalMylist = false;
            }
            else if (playlist is MylistPlaylist mylist)
            {
                IsUserOwnerdMylist = _mylistRepository.IsLoginUserMylistId(mylist.Id);
                IsLoginUserDeflist = mylist.IsDefaultMylist();
                IsWatchAfterLocalMylist = false;
                IsLocalMylist = false;

                Observable.FromEventPattern<MylistItemAddedEventArgs>(
                    h => UserMylistManager.MylistItemAdded += h,
                    h => UserMylistManager.MylistItemAdded -= h
                    )
                    .Subscribe(e =>
                    {
                        var args = e.EventArgs;
                        if (args.MylistId == Playlist.Value.Id)
                        {
                            RefreshCommand.Execute();
                        }
                    })
                    .AddTo(_NavigatingCompositeDisposable);

                Observable.FromEventPattern<MylistItemRemovedEventArgs>(
                    h => UserMylistManager.MylistItemRemoved += h,
                    h => UserMylistManager.MylistItemRemoved -= h
                    )
                    .Subscribe(e =>
                    {
                        var args = e.EventArgs;
                        if (args.MylistId == Playlist.Value.Id)
                        {
                            foreach (var removed in args.SuccessedItems)
                            {
                                var removedItem = MylistItems.FirstOrDefault(x => x.Id == removed);
                                if (removedItem != null)
                                {
                                    MylistItems.Remove(removedItem);
                                }
                            }
                        }
                    })
                    .AddTo(_NavigatingCompositeDisposable);

            }

            MylistItems = await CreateItemsSourceAsync(playlist);
            MaxItemsCount = Playlist.Value.Count;

            if (Playlist.Value != null)
            {
                MylistBookmark = Database.BookmarkDb.Get(Database.BookmarkType.Mylist, Playlist.Value.Id)
                    ?? new Database.Bookmark()
                    {
                        Label = Playlist.Value.Label,
                        Content = Playlist.Value.Id,
                        BookmarkType = Database.BookmarkType.Mylist,
                    };

                FollowToggleButtonService.SetFollowTarget(Playlist.Value as Interfaces.IFollowable);

                RaisePropertyChanged(nameof(MylistBookmark));
            }

            EditMylistGroupCommand.RaiseCanExecuteChanged();
            DeleteMylistCommand.RaiseCanExecuteChanged();

            PageManager.PageTitle = playlist.Label;

        }
       

        private async Task<ICollection<IVideoContent>> CreateItemsSourceAsync(IPlaylist playlist)
        {
            switch (playlist)
            {
                case PlaylistObservableCollection uniquePlaylist:
                    return uniquePlaylist;
                case MylistPlaylist mylist:
                    var source = new MylistIncrementalSource(mylist, _mylistRepository, NgSettings);
                    await source.ResetSource();
                    return new IncrementalLoadingCollection<MylistIncrementalSource, IVideoContent>(source);
                case LocalPlaylist localPlaylist:
                    return new ReadOnlyCollection<IVideoContent>(LocalMylistManager.GetPlaylistItems(localPlaylist).ToList());
                default:
                    throw new ArgumentException();
            }
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = new HohoemaPin()
            {
                Label = Playlist.Value.Label,
                PageType = HohoemaPageType.Mylist,
                Parameter = $"id={Playlist.Value.Id}"
            };

            return true;
        }
    }
    
	public class MylistIncrementalSource : HohoemaIncrementalSourceBase<IVideoContent>
	{
        private readonly IMylist _mylist;
        private readonly MylistRepository _mylistRepository;
        public NGSettings NgSettings { get; }

        public MylistIncrementalSource(
            IMylist mylist,
            MylistRepository mylistRepository
            , NGSettings ngSettings = null
            )
            : base()
        {
            _mylist = mylist;
            _mylistRepository = mylistRepository;
            NgSettings = ngSettings;
        }






        #region Implements HohoemaPreloadingIncrementalSourceBase		



        MylistItemsGetResult _firstResult;

        protected override async Task<int> ResetSourceImpl()
        {
            var result = await _mylistRepository.GetItemsAsync(_mylist, 0, (int)base.OneTimeLoadCount);
            _firstResult = result;


            return result.TotalCount;
        }

        protected override async Task<IAsyncEnumerable<IVideoContent>> GetPagedItemsImpl(int head, int count)
        {
            if (head == 0)
            {
                return _firstResult.Items.ToAsyncEnumerable();
            }
            else if (_firstResult.TotalCount <= head)
            {
                return AsyncEnumerable.Empty<IVideoContent>();
            }
            else
            {
                var result = await _mylistRepository.GetItemsAsync(_mylist, head, count);

                if (result.IsSuccess)
                {
                    return result.Items.ToAsyncEnumerable();
                }
                else
                {
                    return AsyncEnumerable.Empty<IVideoContent>();
                }
            }
        }

        #endregion

    }
}

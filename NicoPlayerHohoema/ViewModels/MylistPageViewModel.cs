using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Commands;
using Mntone.Nico2.Mylist;
using Reactive.Bindings;
using System.Reactive.Linq;
using NicoPlayerHohoema.Models.Helpers;
using System.Threading;
using Microsoft.Practices.Unity;
using Windows.UI;
using Windows.UI.Popups;
using NicoPlayerHohoema.Dialogs;
using System.Collections.Async;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;

namespace NicoPlayerHohoema.ViewModels
{
    public class MylistPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
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
            Services.HohoemaPlaylist hohoemaPlaylist,
            SubscriptionManager subscriptionManager,
            Services.DialogService dialogService,
            Services.Helpers.MylistHelper mylistHelper,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
            )
            : base(pageManager)
        {
            NiconicoSession = niconicoSession;
            MylistProvider = mylistProvider;
            UserProvider = userProvider;
            FollowManager = followManager;
            LoginUserMylistProvider = loginUserMylistProvider;
            NgSettings = ngSettings;
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            SubscriptionManager = subscriptionManager;
            DialogService = dialogService;
            MylistHelper = mylistHelper;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
            Mylist = new ReactiveProperty<Interfaces.IMylist>();
            MylistOrigin = new ReactiveProperty<Services.PlaylistOrigin>();

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



        public NiconicoSession NiconicoSession { get; }
        public MylistProvider MylistProvider { get; }
        public UserProvider UserProvider { get; }
        public FollowManager FollowManager { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public NGSettings NgSettings { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public Services.DialogService DialogService { get; }
        public Services.Helpers.MylistHelper MylistHelper { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }

       
        public ReactiveProperty<Interfaces.IMylist> Mylist { get; private set; }

        public ReactiveProperty<PlaylistOrigin> MylistOrigin { get; }

        private bool _NowProcessFavorite;

        private string _MylistState;
        public string MylistState
        {
            get { return _MylistState; }
            set { SetProperty(ref _MylistState, value); }
        }


        private string _MylistTitle;
        public string MylistTitle
        {
            get { return _MylistTitle; }
            set { SetProperty(ref _MylistTitle, value); }
        }

        private string _MylistDescription;
        public string MylistDescription
        {
            get { return _MylistDescription; }
            set { SetProperty(ref _MylistDescription, value); }
        }

        private bool _IsPublic;
        public bool IsPublic
        {
            get { return _IsPublic; }
            set { SetProperty(ref _IsPublic, value); }
        }

        private Color _ThemeColor;
        public Color ThemeColor
        {
            get { return _ThemeColor; }
            set { SetProperty(ref _ThemeColor, value); }
        }


        public string OwnerUserId { get; private set; }

        private bool _CanEditMylist;
        public bool CanEditMylist
        {
            get { return _CanEditMylist; }
            set { SetProperty(ref _CanEditMylist, value); }
        }

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

        private bool _IsLoginUserMylistWithoutDeflist;
        public bool IsLoginUserMylistWithoutDeflist
        {
            get { return _IsLoginUserMylistWithoutDeflist; }
            set { SetProperty(ref _IsLoginUserMylistWithoutDeflist, value); }
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
                        PageManager.OpenPage(HohoemaPageType.UserInfo, OwnerUserId);
                    }));
            }
        }


        public ReactiveCommand UnregistrationMylistCommand { get; private set; }
        public ReactiveCommand CopyMylistCommand { get; private set; }
        public ReactiveCommand MoveMylistCommand { get; private set; }





        private DelegateCommand<Interfaces.IMylist> _EditMylistGroupCommand;
        public DelegateCommand<Interfaces.IMylist> EditMylistGroupCommand
        {
            get
            {
                return _EditMylistGroupCommand
                    ?? (_EditMylistGroupCommand = new DelegateCommand<Interfaces.IMylist>(async mylist =>
                    {
                        if (mylist is Interfaces.ILocalMylist localMylist)
                        {
                            var resultText = await DialogService.GetTextAsync("プレイリスト名を変更",
                                localMylist.Label,
                                localMylist.Label,
                                (tempName) => !string.IsNullOrWhiteSpace(tempName)
                                );

                            if (!string.IsNullOrWhiteSpace(resultText))
                            {
                                localMylist.Label = resultText;
                                MylistTitle = resultText;
                                PageManager.PageTitle = resultText;
                            }
                        }

                        if (mylist is Models.UserOwnedMylist loginUserMylist)
                        {
                            MylistGroupEditData data = new MylistGroupEditData()
                            {
                                Name = loginUserMylist.Label,
                                Description = loginUserMylist.Description,
                                IsPublic = loginUserMylist.IsPublic,
                                MylistDefaultSort = loginUserMylist.Sort,
                                IconType = loginUserMylist.IconType,
                            };

                            // 成功するかキャンセルが押されるまで繰り返す
                            while (true)
                            {
                                if (true == await DialogService.ShowEditMylistGroupDialogAsync(data))
                                {
                                    loginUserMylist.Label = data.Name;
                                    loginUserMylist.Description = data.Description;
                                    loginUserMylist.IsPublic = data.IsPublic;
                                    loginUserMylist.Sort = data.MylistDefaultSort;
                                    loginUserMylist.IconType = data.IconType;

                                    var result = await LoginUserMylistProvider.UpdateMylist(loginUserMylist);

                                    if (result == Mntone.Nico2.ContentManageResult.Success)
                                    {
                                        MylistTitle = data.Name;
                                        PageManager.PageTitle = MylistTitle;

                                        MylistDescription = data.Description;

                                        await ResetList();
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
                    , mylist => CanEditMylist && !IsLoginUserDeflist && !IsWatchAfterLocalMylist
                    ));
            }
        }



        private DelegateCommand<Interfaces.IMylist> _DeleteMylistCommand;
        public DelegateCommand<Interfaces.IMylist> DeleteMylistCommand
        {
            get
            {
                return _DeleteMylistCommand
                    ?? (_DeleteMylistCommand = new DelegateCommand<Interfaces.IMylist>(async mylist =>
                    {
                        // 確認ダイアログ
                        var mylistOrigin = mylist.ToMylistOrigin();
                        var originText = mylistOrigin == PlaylistOrigin.Local ? "ローカルマイリスト" : "マイリスト";
                        var contentMessage = $"{mylist.Label} を削除してもよろしいですか？（変更は元に戻せません）";

                        var dialog = new MessageDialog(contentMessage, $"{originText}削除の確認");
                        dialog.Commands.Add(new UICommand("削除", async (i) =>
                        {
                            if (mylistOrigin == PlaylistOrigin.Local)
                            {
                                LocalMylistManager.RemoveCommand.Execute(mylist as LocalMylistGroup);
                            }
                            else if (mylistOrigin == PlaylistOrigin.LoginUser)
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
                        if (mylist is Interfaces.ILocalMylist)
                        {
                            return mylist.Id != HohoemaPlaylist.WatchAfterPlaylistId;
                        }
                        else if (mylist is Interfaces.IUserOwnedRemoteMylist remoteOwnedMylist)
                        {
                            return !remoteOwnedMylist.IsDefaultMylist;
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
                        var headItem = IncrementalLoadingItems.FirstOrDefault();
                        if (headItem != null)
                        {
                            HohoemaPlaylist.PlaylistSettings.IsReverseModeEnable = false;
                            HohoemaPlaylist.PlayVideoWithPlaylist(headItem, Mylist.Value);
                        }
                    }));
            }
        }

        private DelegateCommand _PlayAllVideosFromTailCommand;
        public DelegateCommand PlayAllVideosFromTailCommand
        {
            get
            {
                return _PlayAllVideosFromTailCommand
                    ?? (_PlayAllVideosFromTailCommand = new DelegateCommand(() =>
                    {
                        var tailItem = IncrementalLoadingItems.LastOrDefault();
                        if (tailItem != null)
                        {
                            HohoemaPlaylist.PlaylistSettings.IsReverseModeEnable = true;
                            HohoemaPlaylist.PlayVideoWithPlaylist(tailItem, Mylist.Value);
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

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            IsPageNameResolveOnPostNavigatedToAsync = true;

            base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                var payload = MylistPagePayload.FromParameterString<MylistPagePayload>(e.Parameter as string);
                var playableList = await MylistHelper.FindMylist(payload.Id, payload.Origin);

                Mylist.Value = playableList;
                MylistOrigin.Value = playableList.ToMylistOrigin().Value;
            }

            if (Mylist.Value != null)
            {
                MylistBookmark = Database.BookmarkDb.Get(Database.BookmarkType.Mylist, Mylist.Value.Id)
                    ?? new Database.Bookmark()
                    {
                        Label = Mylist.Value.Label,
                        Content = Mylist.Value.Id,
                        BookmarkType = Database.BookmarkType.Mylist,
                    };

                RaisePropertyChanged(nameof(MylistBookmark));
            }

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}

        protected override string ResolvePageName()
        {
            return MylistTitle;
        }

        protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (Mylist.Value == null)
			{
				return;
			}

			CanEditMylist = false;

            var mylistOrigin = Mylist.Value?.ToMylistOrigin();
            IsLoginUserDeflist = false;
            IsWatchAfterLocalMylist = Mylist.Value is Interfaces.ILocalMylist &&
                Mylist.Value?.Id == HohoemaPlaylist.WatchAfterPlaylistId;
            IsUserOwnerdMylist = Mylist.Value is Interfaces.IUserOwnedMylist;
            IsLocalMylist = Mylist.Value is Interfaces.ILocalMylist;

            IsLoginUserMylistWithoutDeflist = false;


            switch (mylistOrigin)
            {
                case PlaylistOrigin.LoginUser:

                    var mylistGroup = UserMylistManager.GetMylistGroup(Mylist.Value.Id);
                    MylistTitle = mylistGroup.Label;
                    MylistDescription = mylistGroup.Description;
                    ThemeColor = mylistGroup.IconType.ToColor();
                    IsPublic = mylistGroup.IsPublic;
                    IsLoginUserDeflist = mylistGroup.IsDeflist;

                    OwnerUserId = mylistGroup.UserId;
                    UserName = NiconicoSession.UserName;

                    CanEditMylist = !IsLoginUserDeflist;

                    if (IsLoginUserDeflist)
                    {
                        MylistState = "とりあえずマイリスト";
                        DeflistRegistrationCapacity = UserMylistManager.DeflistRegistrationCapacity;
                        DeflistRegistrationCount = UserMylistManager.DeflistRegistrationCount;
                    }
                    else
                    {
                        IsLoginUserMylistWithoutDeflist = true;
                        MylistState = IsPublic ? "公開マイリスト" : "非公開マイリスト";
                        MylistRegistrationCapacity = UserMylistManager.MylistRegistrationCapacity;
                        MylistRegistrationCount = UserMylistManager.MylistRegistrationCount;
                    }
                    break;


                case PlaylistOrigin.OtherUser:
                    var otherOwnedMylist = Mylist.Value as OtherOwneredMylist;

                    var response = await MylistProvider.GetMylistGroupDetail(Mylist.Value.Id);
                    var mylistGroupDetail = response.MylistGroup;
                    MylistTitle = otherOwnedMylist.Label;
                    MylistDescription = otherOwnedMylist.Description;
                    IsPublic = true;
                    //ThemeColor = mylistGroupDetail.GetIconType().ToColor();

                    OwnerUserId = mylistGroupDetail.UserId;

                    MylistState = IsPublic ? "公開マイリスト" : "非公開マイリスト";
                    var user = Database.NicoVideoOwnerDb.Get(OwnerUserId);
                    if (user != null)
                    {
                        UserName = user.ScreenName;
                    }
                    else
                    {
                        var userDetail = await UserProvider.GetUser(OwnerUserId);
                        UserName = userDetail.ScreenName;
                    }

                    CanEditMylist = false;

                    if (!otherOwnedMylist.IsFilled)
                    {
                        await MylistProvider.FillMylistGroupVideo(otherOwnedMylist);
                    }

                    break;



                case PlaylistOrigin.Local:

                    MylistTitle = Mylist.Value.Label;
                    OwnerUserId = NiconicoSession.UserId.ToString();
                    UserName = NiconicoSession.UserName;

                    MylistState = "ローカル";

                    CanEditMylist = !IsWatchAfterLocalMylist;

                    break;
                default:
                    break;
            }

			EditMylistGroupCommand.RaiseCanExecuteChanged();
            DeleteMylistCommand.RaiseCanExecuteChanged();

        }



        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
            return new MylistIncrementalSource(Mylist.Value, NgSettings);
        }
    }
    
	public class MylistIncrementalSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
        public MylistIncrementalSource(Interfaces.IMylist list, NGSettings ngSettings = null)
            : base()
        {
            MylistGroupId = list.Id;
            PlayableList = list;
            NgSettings = ngSettings;
        }




        public string MylistGroupId { get; private set; }

        Interfaces.IMylist PlayableList { get; }
        public NGSettings NgSettings { get; }

        

		#region Implements HohoemaPreloadingIncrementalSourceBase		
	

        protected override async Task<IAsyncEnumerable<VideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            // MylistのFillAllVideosAsync で内容が読み込まれるのを待つ
            if (head == 0 && PlayableList.Count > 0)
            {
                using (var cancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    while (PlayableList.Count == 0 && !cancelToken.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                    }

                }
            }

            return PlayableList.Skip(head).Take(count).Select(x =>
                {
                    var vm = App.Current.Container.Resolve<VideoInfoControlViewModel>();
                    var video = Database.NicoVideoDb.Get(x);
                    vm.RawVideoId = x;
                    vm.Data = video;
                    vm.SetTitle(video.Title);
                    return vm;
                })
                .ToAsyncEnumerable();
                
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult(PlayableList.Count);
        }


        #endregion

    }



}

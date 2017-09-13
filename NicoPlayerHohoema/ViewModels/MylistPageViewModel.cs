using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Mvvm;
using Prism.Commands;
using Mntone.Nico2.Mylist;
using System.Collections.ObjectModel;
using Mntone.Nico2;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Diagnostics;
using NicoPlayerHohoema.Util;
using Windows.UI.Xaml;
using Reactive.Bindings.Extensions;
using System.Threading;
using NicoPlayerHohoema.Views.Service;
using Microsoft.Practices.Unity;
using Windows.UI;
using Mntone.Nico2.Live.PlayerStatus;
using System.Runtime.InteropServices.WindowsRuntime;
using NicoPlayerHohoema.Models.Db;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.ViewModels
{
	public class MylistPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{

        ContentSelectDialogService _ContentSelectDialogService;

        public ReactiveProperty<IPlayableList> PlayableList { get; private set; }

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

        #region Commands



        public ReactiveCommand UnregistrationMylistCommand { get; private set; }
        public ReactiveCommand CopyMylistCommand { get; private set; }
        public ReactiveCommand MoveMylistCommand { get; private set; }





        private DelegateCommand _EditMylistGroupCommand;
        public DelegateCommand EditMylistGroupCommand
        {
            get
            {
                return _EditMylistGroupCommand
                    ?? (_EditMylistGroupCommand = new DelegateCommand(async () =>
                    {
                        if (PlayableList.Value.Origin == PlaylistOrigin.Local)
                        {
                            var textInputDialogService = App.Current.Container.Resolve<Views.Service.TextInputDialogService>();
                            var localMylist = PlayableList.Value as LocalMylist;
                            var resultText = await textInputDialogService.GetTextAsync("プレイリスト名を変更",
                                localMylist.Name,
                                localMylist.Name,
                                (tempName) => !string.IsNullOrWhiteSpace(tempName)
                                );

                            if (!string.IsNullOrWhiteSpace(resultText))
                            {
                                localMylist.Name = resultText;
                                MylistTitle = resultText;
                                UpdateTitle(resultText);
                            }
                        }

                        if (PlayableList.Value.Origin == PlaylistOrigin.LoginUser)
                        {
                            var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(PlayableList.Value.Id);
                            MylistGroupEditData data = new MylistGroupEditData()
                            {
                                Name = mylistGroup.Name,
                                Description = mylistGroup.Description,
                                IsPublic = mylistGroup.IsPublic,
                                MylistDefaultSort = mylistGroup.Sort,
                                IconType = mylistGroup.IconType,
                            };

                            var editDialog = App.Current.Container.Resolve<EditMylistGroupDialogService>();

                            // 成功するかキャンセルが押されるまで繰り返す
                            while (true)
                            {
                                if (true == await editDialog.ShowAsync(data))
                                {
                                    var result = await mylistGroup.UpdateMylist(
                                        data.Name,
                                        data.Description,
                                        data.IsPublic,
                                        data.MylistDefaultSort,
                                        data.IconType
                                    );

                                    if (result == Mntone.Nico2.ContentManageResult.Success)
                                    {
                                        MylistTitle = data.Name;
                                        UpdateTitle(MylistTitle);

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
                    , () => CanEditMylist && !IsLoginUserDeflist && !IsWatchAfterLocalMylist
                    ));
            }
        }


        private DelegateCommand _OpenUserPageCommand;
        public DelegateCommand OpenUserPageCommand
        {
            get
            {
                return _OpenUserPageCommand
                    ?? (_OpenUserPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.UserInfo, OwnerUserId);
                    }));
            }
        }

        private DelegateCommand _DeleteMylistCommand;
        public DelegateCommand DeleteMylistCommand
        {
            get
            {
                return _DeleteMylistCommand
                    ?? (_DeleteMylistCommand = new DelegateCommand(async () =>
                    {
                        // 確認ダイアログ
                        var item = PlayableList.Value;
                        var originText = item.Origin == PlaylistOrigin.Local ? "ローカルマイリスト" : "マイリスト";
                        var contentMessage = $"{item.Name} を削除してもよろしいですか？（変更は元に戻せません）";

                        var dialog = new MessageDialog(contentMessage, $"{originText}削除の確認");
                        dialog.Commands.Add(new UICommand("削除", async (i) =>
                        {
                            if (item.Origin == PlaylistOrigin.Local)
                            {
                                await HohoemaApp.Playlist.RemovePlaylist(item as LocalMylist);
                            }
                            else if (item.Origin == PlaylistOrigin.LoginUser)
                            {
                                await HohoemaApp.UserMylistManager.RemoveMylist(item.Id);
                            }

                            PageManager.OpenPage(HohoemaPageType.UserMylist, OwnerUserId);
                        }));

                        dialog.Commands.Add(new UICommand("キャンセル"));
                        dialog.CancelCommandIndex = 1;
                        dialog.DefaultCommandIndex = 1;

                        await dialog.ShowAsync();
                    }, () => CanEditMylist
                    ));
            }
        }


        private DelegateCommand _AddFeedSourceCommand;
        public DelegateCommand AddFeedSourceCommand
        {
            get
            {
                return _AddFeedSourceCommand
                    ?? (_AddFeedSourceCommand = new DelegateCommand(async () =>
                    {
                        if (PlayableList.Value.Origin == PlaylistOrigin.Local) { return; }

                        var targetTitle = this.MylistTitle;
                        var feedGroup = await HohoemaApp.ChoiceFeedGroup(targetTitle + "をフィードに追加");
                        if (feedGroup != null)
                        {
                            if (null != feedGroup.AddMylistFeedSource(targetTitle, this.PlayableList.Value.Id))
                            {
                                // 通知
                                (App.Current as App).PublishInAppNotification(
                                    InAppNotificationPayload.CreateReadOnlyNotification(
                                        content: $"フィードに登録完了\n{feedGroup.Label} に {targetTitle} (マイリスト) を追加しました ",
                                        showDuration: TimeSpan.FromSeconds(7)
                                        ));
                            }
                            else
                            {
                                (App.Current as App).PublishInAppNotification(
                                    InAppNotificationPayload.CreateReadOnlyNotification(
                                        content: $"フィードに失敗\n {feedGroup.Label} に {targetTitle} (マイリスト) を追加できませんでした ",
                                        showDuration: TimeSpan.FromSeconds(7)
                                        ));

                            }
                        }
                    }));
            }
        }



        #endregion



        public MylistPageViewModel(
            HohoemaApp hohoemaApp
            , PageManager pageManager
            )
            : base(hohoemaApp, pageManager, isRequireSignIn: true)
        {
            PlayableList = new ReactiveProperty<IPlayableList>();
            MylistOrigin = new ReactiveProperty<Models.PlaylistOrigin>();

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
                        || HohoemaApp.FollowManager.CanMoreAddFollow(FollowItemType.Mylist);


                    _NowProcessFavorite = false;
                })
                .AddTo(_CompositeDisposable);


            UnregistrationMylistCommand = SelectedItems.ObserveProperty(x => x.Count)
                .Where(_ => this.CanEditMylist)
                .Select(x => x > 0)
                .ToReactiveCommand(false);

            UnregistrationMylistCommand.Subscribe(async _ =>
            {
                if (PlayableList.Value.Origin == PlaylistOrigin.Local)
                {
                    var localMylist = PlayableList.Value as LocalMylist;
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
                                , withRefresh: false /* あとでまとめてリフレッシュするのでここでは OFF */);

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

                            Debug.WriteLine($"{video.Title}[{video.RawVideoId}]:{unregistrationResult.ToString()}");
                        }

                        // 登録解除結果を得るためリフレッシュ
                        await mylistGroup.Refresh();


                        // ユーザーに結果を通知
                        var titleText = $"「{mylistGroup.Name}」から {successCount}件 の動画が登録解除されました";
                        var toastService = App.Current.Container.Resolve<ToastNotificationService>();
                        var resultText = $"";
                        if (failedCount > 0)
                        {
                            resultText += $"\n登録解除に失敗した {failedCount}件 は選択されたままです";
                        }
                        toastService.ShowText(titleText, resultText);

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

        }



		private async Task<bool> FavoriteMylist()
		{
			if (PlayableList.Value == null) { return false; }
            if (PlayableList.Value.Origin != PlaylistOrigin.OtherUser) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.AddFollow(FollowItemType.Mylist, PlayableList.Value.Id, PlayableList.Value.Name);

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

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                var payload = MylistPagePayload.FromParameterString<MylistPagePayload>(e.Parameter as string);
                var playableList = await HohoemaApp.GetPlayableList(payload.Id, payload.Origin);

                PlayableList.Value = playableList;
                MylistOrigin.Value = playableList.Origin;
            }


            await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}

		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (PlayableList.Value == null)
			{
				return;
			}

			CanEditMylist = false;


			// お気に入り状態の取得
			_NowProcessFavorite = true;

			var favManager = HohoemaApp.FollowManager;
			IsFavoriteMylist.Value = favManager.IsFollowItem(FollowItemType.Mylist, PlayableList.Value.Id);

			CanChangeFavoriteMylistState.Value =
				IsFavoriteMylist.Value == true
				|| favManager.CanMoreAddFollow(FollowItemType.Mylist);

			_NowProcessFavorite = false;


            
            IsLoginUserDeflist = false;
            IsWatchAfterLocalMylist = PlayableList.Value.Origin == PlaylistOrigin.Local &&
                PlayableList.Value.Id == HohoemaPlaylist.WatchAfterPlaylistId;
            IsUserOwnerdMylist = HohoemaApp.UserMylistManager.HasMylistGroup(PlayableList.Value.Id) || IsWatchAfterLocalMylist;
            IsLocalMylist = PlayableList.Value.Origin == PlaylistOrigin.Local;

            IsLoginUserMylistWithoutDeflist = false;


            switch (PlayableList.Value.Origin)
            {
                case PlaylistOrigin.LoginUser:

                    var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(PlayableList.Value.Id);
                    MylistTitle = mylistGroup.Name;
                    MylistDescription = mylistGroup.Description;
                    ThemeColor = mylistGroup.IconType.ToColor();
                    IsPublic = mylistGroup.IsPublic;
                    IsLoginUserDeflist = mylistGroup.IsDeflist;

                    OwnerUserId = mylistGroup.UserId;
                    UserName = HohoemaApp.LoginUserName;

                    CanEditMylist = !IsLoginUserDeflist;

                    if (IsLoginUserDeflist)
                    {
                        MylistState = "とりあえずマイリスト";
                        DeflistRegistrationCapacity = HohoemaApp.UserMylistManager.DeflistRegistrationCapacity;
                        DeflistRegistrationCount = HohoemaApp.UserMylistManager.DeflistRegistrationCount;
                    }
                    else
                    {
                        IsLoginUserMylistWithoutDeflist = true;
                        MylistState = IsPublic ? "公開マイリスト" : "非公開マイリスト";
                        MylistRegistrationCapacity = HohoemaApp.UserMylistManager.MylistRegistrationCapacity;
                        MylistRegistrationCount = HohoemaApp.UserMylistManager.MylistRegistrationCount;
                    }
                    break;


                case PlaylistOrigin.OtherUser:
                    var response = await HohoemaApp.ContentFinder.GetMylistGroupDetail(PlayableList.Value.Id);
                    var mylistGroupDetail = response.MylistGroup;
                    MylistTitle = mylistGroupDetail.Name;
                    MylistDescription = mylistGroupDetail.Description;
                    IsPublic = mylistGroupDetail.IsPublic;
                    ThemeColor = mylistGroupDetail.GetIconType().ToColor();

                    OwnerUserId = mylistGroupDetail.UserId;

                    MylistState = IsPublic ? "公開マイリスト" : "非公開マイリスト";


                    var user = await UserInfoDb.GetAsync(OwnerUserId);
                    if (user != null)
                    {
                        UserName = user.Name;
                    }
                    else
                    {
                        await Task.Delay(500);
                        var userDetail = await HohoemaApp.ContentFinder.GetUserDetail(OwnerUserId);
                        UserName = userDetail.Nickname;
                    }

                    CanEditMylist = false;

                    break;



                case PlaylistOrigin.Local:

                    MylistTitle = PlayableList.Value.Name;
                    OwnerUserId = HohoemaApp.LoginUserId.ToString();
                    UserName = HohoemaApp.LoginUserName;

                    MylistState = "ローカル";

                    CanEditMylist = !IsWatchAfterLocalMylist;

                    break;
                default:
                    break;
            }




            UpdateTitle(MylistTitle);


			EditMylistGroupCommand.RaiseCanExecuteChanged();
            DeleteMylistCommand.RaiseCanExecuteChanged();

        }



        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
            if (PlayableList.Value.Origin == PlaylistOrigin.Local)
            {
                return new LocalMylistIncrementalSource(PlayableList.Value as LocalMylist, HohoemaApp, PageManager);
            }
			else if (PlayableList.Value.Origin == PlaylistOrigin.LoginUser && PlayableList.Value.Id == "0")
			{
				return new DeflistMylistIncrementalSource(HohoemaApp, PageManager);
			}
			else
			{
				return new MylistIncrementalSource(PlayableList.Value.Id, HohoemaApp, PageManager);
			}
		}



	}


	public class DeflistMylistIncrementalSource : HohoemaPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
	{

		PageManager _PageManager;
		MylistGroupInfo _MylistGroupInfo;
		public DeflistMylistIncrementalSource(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, "DeflistMylist")
		{
			_PageManager = pageManager;
			_MylistGroupInfo = HohoemaApp.UserMylistManager.GetMylistGroup("0");

		}



		#region Implements HohoemaPreloadingIncrementalSourceBase		

		protected override async Task<int> HohoemaPreloadingResetSourceImpl()
		{
			await _MylistGroupInfo.Refresh();
			return await Task.FromResult(_MylistGroupInfo.ItemCount);
		}

		protected override async Task Preload(int start, int count)
		{
			try
			{
				var items = _MylistGroupInfo.PlaylistItems;
				foreach (var item in items.Skip(start).Take(count))
				{
					await HohoemaApp.MediaManager.GetNicoVideoAsync(item.ContentId);
				}
			}
			catch (Exception ex)
			{
				TriggerError(ex);
			}
		}

		

		protected override async Task<IEnumerable<VideoInfoControlViewModel>> HohoemaPreloadingGetPagedItemsImpl(int head, int count)
		{
			List<VideoInfoControlViewModel> list = new List<VideoInfoControlViewModel>();

			var items = _MylistGroupInfo.PlaylistItems;

			if (items.Count <= head)
			{
				return list;
			}

			foreach (var item in items.Skip(head).Take(count))
			{
				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.ContentId);

				var videoVM = new VideoInfoControlViewModel(nicoVideo, _PageManager, isNgEnabled: false, playlistItem: item);
				list.Add(videoVM);
			}

			return list;
		}


		#endregion
	}

	public class MylistIncrementalSource : HohoemaPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public string MylistGroupId { get; private set; }

		PageManager _PageManager;

		public MylistIncrementalSource(string mylistGroupId, HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, "Mylist:"+mylistGroupId)
		{
			MylistGroupId = mylistGroupId;

			_PageManager = pageManager;
		}




		#region Implements HohoemaPreloadingIncrementalSourceBase		

		protected override async Task<int> HohoemaPreloadingResetSourceImpl()
		{
			var count = 0;
			var mylistManager = HohoemaApp.UserMylistManager;
			if (mylistManager.HasMylistGroup(MylistGroupId))
			{
				var mylistGroup = mylistManager.GetMylistGroup(MylistGroupId);
				await mylistGroup.Refresh();
				count = mylistGroup.ItemCount;
			}
			else
			{
				var res = await HohoemaApp.ContentFinder.GetMylistGroupVideo(MylistGroupId, 0, 1);
				count = (int)res.GetTotalCount();
			}

			return count;
		}


		protected override async Task Preload(int start, int count)
		{
			try
			{
				var mylistManager = HohoemaApp.UserMylistManager;
				if (mylistManager.HasMylistGroup(MylistGroupId))
				{
					var mylistGroup = mylistManager.GetMylistGroup(MylistGroupId);
					var items = mylistGroup.PlaylistItems;
					foreach (var videoId in items.Skip((int)start).Take((int)count))
					{
						//await HohoemaApp.MediaManager.GetNicoVideoAsync(videoId);
					}
				}
				else
				{
					var res = await HohoemaApp.ContentFinder.GetMylistGroupVideo(MylistGroupId, (uint)start, (uint)count);

					if (res.GetCount() > 0)
					{
						foreach (var item in res.MylistVideoInfoItems)
						{
							//await HohoemaApp.MediaManager.GetNicoVideoAsync(item.Video.Id);
						}
					}
				}
			}
			catch (Exception ex)
			{
				TriggerError(ex);
			}
		}


		
		protected override async Task<IEnumerable<VideoInfoControlViewModel>> HohoemaPreloadingGetPagedItemsImpl(int head, int count)
		{
			List<VideoInfoControlViewModel> list = new List<VideoInfoControlViewModel>();

			if (MylistGroupId == null || MylistGroupId == "0")
			{
				throw new Exception();
			}

			var mylistManager = HohoemaApp.UserMylistManager;
			if (mylistManager.HasMylistGroup(MylistGroupId))
			{
				var mylistGroup = mylistManager.GetMylistGroup(MylistGroupId);
				var items = mylistGroup.PlaylistItems;

				if (items.Count <= head)
				{
					return list;
				}

				foreach (var item in items.Skip(head).Take(count))
				{
					var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.ContentId, true);
                    var videoListItemVM = new VideoInfoControlViewModel(nicoVideo, _PageManager, isNgEnabled:false, playlistItem:item);
					list.Add(videoListItemVM);
				}
			}
			else
			{
				var res = await HohoemaApp.ContentFinder.GetMylistGroupVideo(MylistGroupId, (uint)head, (uint)count);

				if (res.GetCount() > 0)
				    foreach (var item in res.MylistVideoInfoItems)
				    {
					    var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.Video.Id, false);
                        nicoVideo.PreSetTitle(item.Video.Title);
					    list.Add(new VideoInfoControlViewModel(item, nicoVideo, _PageManager));
				    }
			}

			return list;
		}


		#endregion

	}

    public class LocalMylistIncrementalSource : HohoemaPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
    {
        LocalMylist LocalMylist { get; }
        PageManager _PageManager;

        public LocalMylistIncrementalSource(LocalMylist localMylist , HohoemaApp hohoemaApp, PageManager pageManager)
            : base(hohoemaApp, "LocalMylist:" + localMylist.Name)
        {
            LocalMylist = localMylist;
            _PageManager = pageManager;
        }




        #region Implements HohoemaPreloadingIncrementalSourceBase		

        protected override Task<int> HohoemaPreloadingResetSourceImpl()
        {
            return Task.FromResult(LocalMylist.PlaylistItems.Count);
        }


        protected override Task Preload(int start, int count)
        {
            return Task.CompletedTask;
        }



        protected override async Task<IEnumerable<VideoInfoControlViewModel>> HohoemaPreloadingGetPagedItemsImpl(int head, int count)
        {
            List<VideoInfoControlViewModel> list = new List<VideoInfoControlViewModel>();

            foreach (var item in LocalMylist.PlaylistItems.Skip(head).Take(count))
            {
                var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.ContentId, true);
                list.Add(new VideoInfoControlViewModel(nicoVideo, _PageManager, isNgEnabled: false, playlistItem: item));
            }

            return list;
        }


        #endregion

    }



}

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

namespace NicoPlayerHohoema.ViewModels
{
	public class MylistPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{

        ContentSelectDialogService _ContentSelectDialogService;

        public MylistPageViewModel(
            HohoemaApp hohoemaApp
            , PageManager pageManager
            , Views.Service.MylistRegistrationDialogService mylistDialogService
            , Views.Service.ContentSelectDialogService contentSelectDialogService
            )
			: base(hohoemaApp, pageManager, mylistDialogService, isRequireSignIn: true)
		{
            _ContentSelectDialogService = contentSelectDialogService;

            IsFavoriteMylist = new ReactiveProperty<bool>(mode:ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);
			CanChangeFavoriteMylistState = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);


			IsFavoriteMylist
				.Where(x => MylistGroupId != null)
				.Subscribe(async x => 
				{
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
				var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(MylistGroupId);

				var items = SelectedItems.ToArray();


				var action = AsyncInfo.Run<uint>(async (cancelToken, progress) =>
				{
					uint progressCount = 0;
					int successCount = 0;
					int failedCount = 0;

					Debug.WriteLine($"マイリスト登録解除を開始...");
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

					Debug.WriteLine($"マイリスト登録解除完了---------------");
				});

				await PageManager.StartNoUIWork("マイリスト登録解除", items.Length, () => action);

				
			});

			CopyMylistCommand = SelectedItems.ObserveProperty(x => x.Count)
				.Where(_ => this.CanEditMylist)
				.Select(x => x > 0)
				.ToReactiveCommand(false);

			CopyMylistCommand.Subscribe(async _ => 
			{
				var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(MylistGroupId);
				// ターゲットのマイリストを選択する
				var targetMylist = await MylistDialogService
				.ShowSelectSingleMylistDialog(
					SelectedItems.Count
					, hideMylistGroupId: mylistGroup.GroupId
				);

				if (targetMylist == null) { return; }



				// すでにターゲットのマイリストに登録されている動画を除外してコピーする
				var items = SelectedItems
					.Where(x => !targetMylist.CheckRegistratedVideoId(x.RawVideoId))
					.ToList();

				var action = AsyncInfo.Run<uint>(async (cancelToken, progress) =>
				{
					Debug.WriteLine($"マイリストのコピーを開始...");

					var result = await mylistGroup.CopyMylistTo(
						   targetMylist
						   , items.Select(video => video.RawVideoId).ToArray()
						   );


					Debug.WriteLine($"copy mylist {items.Count} item from {mylistGroup.Name} to {targetMylist.Name} : {result.ToString()}");

					// ユーザーに結果を通知
					var toastService = App.Current.Container.Resolve<ToastNotificationService>();

					string titleText;
					string contentText;
					if (result == ContentManageResult.Success)
					{
						titleText = $"「{targetMylist.Name}」に {SelectedItems.Count}件 コピーしました";
						contentText = $" {SelectedItems.Count}件 の動画をコピーしました";

						progress.Report((uint)items.Count);
					}
					else
					{
						titleText = $"マイリストコピーに失敗";
						contentText = $"時間を置いてからやり直してみてください";
					}

					toastService.ShowText(titleText, contentText);



					// 成功した場合は選択状態を解除
					if (result == ContentManageResult.Success)
					{
						ClearSelection();
					}

					Debug.WriteLine($"マイリストのコピー完了...");
				});

				await PageManager.StartNoUIWork("マイリストのコピー", items.Count, () => action);
			});


			MoveMylistCommand = SelectedItems.ObserveProperty(x => x.Count)
				.Where(_ => this.CanEditMylist)
				.Select(x => x > 0)
				.ToReactiveCommand(false);

			MoveMylistCommand.Subscribe(async _ =>
			{
				var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(MylistGroupId);

				// ターゲットのマイリストを選択する
				var targetMylist = await MylistDialogService
					.ShowSelectSingleMylistDialog(
						SelectedItems.Count
						, hideMylistGroupId: mylistGroup.GroupId
					);

				if (targetMylist == null) { return; }



				// すでにターゲットのマイリストに登録されている動画を除外してコピーする
				var items = SelectedItems
					.Where(x => !targetMylist.CheckRegistratedVideoId(x.RawVideoId))
					.ToList();

				Debug.WriteLine($"マイリストの移動を開始...");

				var result = await mylistGroup.MoveMylistTo(
					   targetMylist
					   , items.Select(video => video.RawVideoId).ToArray()
					   );

				Debug.WriteLine($"move mylist {items.Count} item from {mylistGroup.Name} to {targetMylist.Name} : {result.ToString()}");

				// ユーザーに結果を通知
				var toastService = App.Current.Container.Resolve<ToastNotificationService>();

				string titleText;
				string contentText;
				if (result == ContentManageResult.Success)
				{
					titleText = $"「{targetMylist.Name}」に {SelectedItems.Count}件 移動しました";
					contentText = $" {SelectedItems.Count}件 の動画を移動しました";
				}
				else
				{
					titleText = $"マイリスト移動に失敗";
					contentText = $"時間を置いてからやり直してみてください";
				}

				toastService.ShowText(titleText, contentText);



				// 成功した場合は選択状態を解除
				if (result == ContentManageResult.Success)
				{
					// 移動元のマイリストからは削除されているはず
					foreach (var item in SelectedItems)
					{
						if (!mylistGroup.CheckRegistratedVideoId(item.RawVideoId))
						{
							IncrementalLoadingItems.Remove(item);
						}
						else
						{
							throw new Exception();
						}
					}

					ClearSelection();
				}

				Debug.WriteLine($"マイリストの移動完了...");
			});

		}



		private async Task<bool> FavoriteMylist()
		{
			if (MylistGroupId == null) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.AddFollow(FollowItemType.Mylist, MylistGroupId, MylistTitle);

			return result == ContentManageResult.Success || result == ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteMylist()
		{
			if (MylistGroupId == null) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.RemoveFollow(FollowItemType.Mylist, MylistGroupId);

			return result == ContentManageResult.Success;

		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				MylistGroupId = e.Parameter as string;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return base.NavigatedToAsync(cancelToken, e, viewModelState);
		}

		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (MylistGroupId == null)
			{
				return;
			}

			CanEditMylist = false;


			// お気に入り状態の取得
			_NowProcessFavorite = true;

			var favManager = HohoemaApp.FollowManager;
			IsFavoriteMylist.Value = favManager.IsFollowItem(FollowItemType.Mylist, MylistGroupId);

			CanChangeFavoriteMylistState.Value =
				IsFavoriteMylist.Value == true
				|| favManager.CanMoreAddFollow(FollowItemType.Mylist);

			_NowProcessFavorite = false;




            IsUserOwnerdMylist = HohoemaApp.UserMylistManager.HasMylistGroup(MylistGroupId);

            
            IsLoginUserDeflist = false;

			try
			{
				if (IsUserOwnerdMylist)
				{
					var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(MylistGroupId);
					MylistTitle = mylistGroup.Name;
					MylistDescription = mylistGroup.Description;
					ThemeColor = mylistGroup.IconType.ToColor();
					IsPublic = mylistGroup.IsPublic;
					IsLoginUserDeflist = mylistGroup.IsDeflist;

					OwnerUserId = mylistGroup.UserId;
					UserName = HohoemaApp.LoginUserName;

                    CanEditMylist = !IsLoginUserDeflist;
                }
                else
				{
					var response = await HohoemaApp.ContentFinder.GetMylistGroupDetail(MylistGroupId);
					var mylistGroupDetail = response.MylistGroup;
					MylistTitle = mylistGroupDetail.Name;
					MylistDescription = mylistGroupDetail.Description;
					IsPublic = mylistGroupDetail.IsPublic;
					ThemeColor = mylistGroupDetail.GetIconType().ToColor();

					OwnerUserId = mylistGroupDetail.UserId;

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
                }

			}
			catch
			{

			}

			UpdateTitle(MylistTitle);


			EditMylistGroupCommand.RaiseCanExecuteChanged();
		}

		

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			if (MylistGroupId == "0")
			{
				return new DeflistMylistIncrementalSource(HohoemaApp, PageManager);
			}
			else
			{
				return new MylistIncrementalSource(MylistGroupId, HohoemaApp, PageManager);
			}
		}


		private DelegateCommand _EditMylistGroupCommand;
		public DelegateCommand EditMylistGroupCommand
		{
			get
			{
				return _EditMylistGroupCommand
					?? (_EditMylistGroupCommand = new DelegateCommand(async () =>
					{
						var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(MylistGroupId);
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
					, () => CanEditMylist && !IsLoginUserDeflist
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
                        if (CanEditMylist)
                        {
                            var result = await HohoemaApp.UserMylistManager.RemoveMylist(this.MylistGroupId);
                            
                            if (result == ContentManageResult.Success)
                            {
                                PageManager.ForgetLastPage();
                                if (PageManager.NavigationService.CanGoBack())
                                {
                                    PageManager.NavigationService.GoBack();
                                }
                                else
                                {
                                    PageManager.OpenPage(HohoemaPageType.UserMylist);
                                }
                            }
                        }
                    }));
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
                        var result = await _ContentSelectDialogService.ShowDialog(new ContentSelectDialogDefaultSet()
                        {
                            DialogTitle = MylistTitle + "をフィードに追加",
                            ChoiceListTitle = "フィードグループ",
                            ChoiceList = HohoemaApp.FeedManager.FeedGroups
                                .Select(x => new SelectDialogPayload() { Id = x.Id.ToString(), Label = x.Label })
                                .ToList()
                        });

                        if (result != null)
                        {
                            var feedGroup = HohoemaApp.FeedManager.GetFeedGroup(Guid.Parse(result.Id));
                            feedGroup.AddMylistFeedSource(MylistTitle, MylistGroupId);
                        }
                    }));
            }
        }



        public ReactiveCommand UnregistrationMylistCommand { get; private set; }
		public ReactiveCommand CopyMylistCommand { get; private set; }
		public ReactiveCommand MoveMylistCommand { get; private set; }




		private bool _NowProcessFavorite;



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

		public string MylistGroupId { get; private set; }

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

		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}

		public ReactiveProperty<bool> IsFavoriteMylist { get; private set; }
		public ReactiveProperty<bool> CanChangeFavoriteMylistState { get; private set; }

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
				var items = _MylistGroupInfo.VideoItems;
				foreach (var item in items.Skip(start).Take(count))
				{
					await HohoemaApp.MediaManager.GetNicoVideoAsync(item);
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

			var items = _MylistGroupInfo.VideoItems;

			if (items.Count <= head)
			{
				return list;
			}

			foreach (var videoId in items.Skip(head).Take(count))
			{
				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(videoId);

				var videoVM = new VideoInfoControlViewModel(nicoVideo, _PageManager);
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
					var items = mylistGroup.VideoItems;
					foreach (var videoId in items.Skip((int)start).Take((int)count))
					{
						await HohoemaApp.MediaManager.GetNicoVideoAsync(videoId);
					}
				}
				else
				{
					var res = await HohoemaApp.ContentFinder.GetMylistGroupVideo(MylistGroupId, (uint)start, (uint)count);

					if (res.GetCount() > 0)
					{
						foreach (var item in res.MylistVideoInfoItems)
						{
							await HohoemaApp.MediaManager.GetNicoVideoAsync(item.Video.Id);
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
				var items = mylistGroup.VideoItems;

				if (items.Count <= head)
				{
					return list;
				}

				foreach (var videoId in items.Skip(head).Take(count))
				{
					var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(videoId);
					var videoListItemVM = new VideoInfoControlViewModel(nicoVideo, _PageManager);
					list.Add(videoListItemVM);
				}
			}
			else
			{
				var res = await HohoemaApp.ContentFinder.GetMylistGroupVideo(MylistGroupId, (uint)head, (uint)count);

				if (res.GetCount() > 0)
				foreach (var item in res.MylistVideoInfoItems)
				{
					var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.Video.Id);
					list.Add(new VideoInfoControlViewModel(item, nicoVideo, _PageManager));
				}
			}

			return list;
		}


		#endregion

	}




}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using System.Collections.ObjectModel;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedGroupPageViewModel : HohoemaViewModelBase
	{
		public IFeedGroup FeedGroup { get; private set; }

		public ReactiveProperty<string> FeedGroupName { get; private set; }

		public ReactiveProperty<bool> IsDeleted { get; private set; }

		public ObservableCollection<FeedItemSourceListItem> MylistFeedSources { get; private set; }
		public ObservableCollection<FeedItemSourceListItem> TagFeedSources { get; private set; }
		public ObservableCollection<FeedItemSourceListItem> UserFeedSources { get; private set; }

		public ReadOnlyReactiveProperty<bool> HasMylistFeedSource { get; private set; }
		public ReadOnlyReactiveProperty<bool> HasTagFeedSource { get; private set; }
		public ReadOnlyReactiveProperty<bool> HasUserFeedSource { get; private set; }


		public ReactiveProperty<bool> SelectFromFavItems { get; private set; }


		public ReactiveProperty<FollowItemInfo> SelectedFavInfo { get; private set; }

		public ObservableCollection<FollowItemInfo> MylistFavItems { get; private set; }
		public ObservableCollection<FollowItemInfo> TagFavItems { get; private set; }
		public ObservableCollection<FollowItemInfo> UserFavItems { get; private set; }

		public ReactiveProperty<FollowItemType> FavItemType { get; private set; }
		public ReactiveProperty<string> FeedSourceId { get; private set; }
		public ReactiveProperty<string> FeedSourceItemName { get; private set; }
		public ReactiveProperty<bool> ExistFeedSource { get; private set; }
		public ReactiveProperty<bool> IsPublicFeedSource { get; private set; }
		

		public Views.Service.ContentSelectDialogService ContentSelectDialogService { get; private set; }

		public FeedGroupPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.ContentSelectDialogService contentSelectDialogService) 
			: base(hohoemaApp, pageManager)
		{
			ContentSelectDialogService = contentSelectDialogService;

			IsDeleted = new ReactiveProperty<bool>();

			FeedGroupName = new ReactiveProperty<string>();
			MylistFeedSources = new ObservableCollection<FeedItemSourceListItem>();
			TagFeedSources = new ObservableCollection<FeedItemSourceListItem>();
			UserFeedSources = new ObservableCollection<FeedItemSourceListItem>();

			HasMylistFeedSource = MylistFeedSources.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReadOnlyReactiveProperty();
			HasTagFeedSource = TagFeedSources.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReadOnlyReactiveProperty();
			HasUserFeedSource = UserFeedSources.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReadOnlyReactiveProperty();


			MylistFavItems = new ObservableCollection<FollowItemInfo>();
			TagFavItems = new ObservableCollection<FollowItemInfo>();
			UserFavItems = new ObservableCollection<FollowItemInfo>();

			SelectFromFavItems = new ReactiveProperty<bool>(true);
			SelectedFavInfo = new ReactiveProperty<FollowItemInfo>();

			FavItemType = new ReactiveProperty<FollowItemType>();
			FeedSourceId = new ReactiveProperty<string>();
			FeedSourceItemName = new ReactiveProperty<string>();
			ExistFeedSource = new ReactiveProperty<bool>();
			IsPublicFeedSource = new ReactiveProperty<bool>();

			CanUseFeedSource = Observable.CombineLatest(
				ExistFeedSource,
				IsPublicFeedSource
				)
				.Select(x => x.All(y => y))
				.ToReactiveProperty();

			FavItemType.Subscribe(x => 
			{
				FeedSourceId.Value = "";
				ExistFeedSource.Value = false;

				FeedSourceItemName.Value = "";


				// お気に入りアイテムがある場合は、「お気に入りから選択」をデフォルトに
				switch (x)
				{
					case FollowItemType.Tag:
						SelectFromFavItems.Value = TagFavItems.Count > 0;
						break;
					case FollowItemType.Mylist:
						SelectFromFavItems.Value = MylistFavItems.Count > 0;
						break;
					case FollowItemType.User:
						SelectFromFavItems.Value = UserFavItems.Count > 0;
						break;
					default:
						break;
				}
				

			});

			FeedSourceId.ToUnit()
				.Subscribe(_ => 
				{
					ExistFeedSource.Value = false;
					FeedSourceItemName.Value = "";
				});

			Observable.Merge(
				SelectFromFavItems.ToUnit(),
				
				SelectedFavInfo.ToUnit(),

				FavItemType.ToUnit(),
				FeedSourceId.ToUnit().Throttle(TimeSpan.FromSeconds(1))				
				)
				.Subscribe(async x => 
				{
					if (SelectFromFavItems.Value)
					{
						ExistFeedSource.Value = SelectedFavInfo.Value != null;
						IsPublicFeedSource.Value = true;
						FeedSourceItemName.Value = "";
						return;
					}

					ExistFeedSource.Value = false;

					if (FavItemType.Value == FollowItemType.Tag)
					{
						ExistFeedSource.Value = !string.IsNullOrWhiteSpace(FeedSourceId.Value);
						IsPublicFeedSource.Value = true;
						FeedSourceItemName.Value = FeedSourceId.Value;
					}
					else
					{
						if (string.IsNullOrWhiteSpace(FeedSourceId.Value))
						{
							ExistFeedSource.Value = false;
						}
						else
						{
							if (FavItemType.Value == FollowItemType.Mylist)
							{
								try
								{
									var mylistRes = await HohoemaApp.ContentFinder.GetMylistGroupDetail(FeedSourceId.Value);
									var mylist = mylistRes?.MylistGroup;
									
									if (mylist != null)
									{
										ExistFeedSource.Value = true;
										IsPublicFeedSource.Value = mylist.IsPublic;
										FeedSourceItemName.Value = Mntone.Nico2.StringExtention.DecodeUTF8(mylist.Name);
									}
								}
								catch
								{
									ExistFeedSource.Value = false;
								}

							}
							else if (FavItemType.Value == FollowItemType.User)
							{
								try
								{
									var user = await HohoemaApp.ContentFinder.GetUserDetail(FeedSourceId.Value);
									if (user != null)
									{
										ExistFeedSource.Value = true;
										IsPublicFeedSource.Value = !user.IsOwnerVideoPrivate;
										FeedSourceItemName.Value = user.Nickname;
									}
								}
								catch
								{
									ExistFeedSource.Value = false;
								}
							}

							if (!ExistFeedSource.Value)
							{
								IsPublicFeedSource.Value = false;
								FeedSourceItemName.Value = "";
							}
						}
					}
				});

			AddFeedCommand = 
				Observable.CombineLatest(
					ExistFeedSource,
					IsPublicFeedSource
					)
				.Select(x => x.All(y => y == true))
				.ToReactiveCommand();

			AddFeedCommand.Subscribe(_ =>
			{
				string name = "";
				string id = "";

				if (SelectFromFavItems.Value)
				{
					var favInfo = SelectedFavInfo.Value;
					name = favInfo.Name;
					id = favInfo.Id;

					if (favInfo.FollowItemType != FavItemType.Value)
					{
						throw new Exception();
					}
				}
				else
				{
					// idからMylistGroupを引く
					// 公開されていない場合にはエラー
					id = FeedSourceId.Value;
					name = FeedSourceItemName.Value;

					FeedSourceItemName.Value = "";
					FeedSourceId.Value = "";
				}

				var favManager = HohoemaApp.FollowManager;
				var feedManager = HohoemaApp.FeedManager;
				IFeedSource feedSource;
				switch (FavItemType.Value)
				{
					case FollowItemType.Tag:

						feedSource = FeedGroup.AddTagFeedSource(id);
						if (feedSource != null)
						{
							var favInfo = favManager.Tag.FollowInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								TagFavItems.Remove(favInfo);
							}

							TagFeedSources.Add(new FeedItemSourceListItem(feedSource, this));


						}

						break;
					case FollowItemType.Mylist:

						feedSource = FeedGroup.AddMylistFeedSource(name, id);
						if (feedSource != null)
						{
							var favInfo = favManager.Mylist.FollowInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								MylistFavItems.Remove(favInfo);
							}

							MylistFeedSources.Add(new FeedItemSourceListItem(feedSource, this));
						}

						break;
					case FollowItemType.User:

						feedSource = FeedGroup.AddUserFeedSource(name, id);
						if (feedSource != null)
						{
							var favInfo = favManager.User.FollowInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								UserFavItems.Remove(favInfo);
							}

							UserFeedSources.Add(new FeedItemSourceListItem(feedSource, this));
						}

						break;
					default:
						break;
				}

				HohoemaApp.FeedManager.SaveOne(FeedGroup);

			});

			RenameApplyCommand = FeedGroupName
				.Where(x => HohoemaApp.FeedManager != null && x != null)
				.Select(x => HohoemaApp.FeedManager.CanAddLabel(x))
				.ToReactiveCommand();

			RenameApplyCommand.Subscribe(async _ => 
			{
				if (await FeedGroup.Rename(FeedGroupName.Value))
				{
					UpdateTitle(FeedGroup.Label);
				}

				FeedGroupName.ForceNotify();
			});
		}




		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			FeedGroup = null;

			if (e.Parameter is Guid)
			{
				var feedGroupId = (Guid)e.Parameter;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}
			else if (e.Parameter is string)
			{
				var feedGroupId = Guid.Parse(e.Parameter as string);

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}

			IsDeleted.Value = FeedGroup == null;

			if (FeedGroup != null)
			{
				UpdateTitle(FeedGroup.Label);

				FeedGroupName.Value = FeedGroup.Label;

				MylistFeedSources.Clear();
				foreach (var mylistFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FollowItemType == FollowItemType.Mylist))
				{
					MylistFeedSources.Add(new FeedItemSourceListItem(mylistFeedSrouce, this));
				}

				MylistFavItems.Clear();
				foreach (var mylistFavInfo in HohoemaApp.FollowManager.Mylist.FollowInfoItems.Where(x => MylistFeedSources.All(y => x.Id != y.FeedSource.Id)))
				{
					MylistFavItems.Add(mylistFavInfo);
				}

				TagFeedSources.Clear();
				foreach (var tagFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FollowItemType == FollowItemType.Tag))
				{
					TagFeedSources.Add(new FeedItemSourceListItem(tagFeedSrouce, this));
				}

				TagFavItems.Clear();
				foreach (var tagFavInfo in HohoemaApp.FollowManager.Tag.FollowInfoItems.Where(x => TagFeedSources.All(y => x.Id != y.FeedSource.Id)))
				{
					TagFavItems.Add(tagFavInfo);
				}

				UserFeedSources.Clear();
				foreach (var userFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FollowItemType == FollowItemType.User))
				{
					UserFeedSources.Add(new FeedItemSourceListItem(userFeedSrouce, this));
				}

				UserFavItems.Clear();
				foreach (var userFavInfo in HohoemaApp.FollowManager.User.FollowInfoItems.Where(x => UserFeedSources.All(y => x.Id != y.FeedSource.Id)))
				{
					UserFavItems.Add(userFavInfo);
				}
			}







			
		}


		public ReactiveCommand AddFeedCommand { get; private set; }

		private DelegateCommand _RemoveFeedGroupCommand;
		public DelegateCommand RemoveFeedGroupCommand
		{
			get
			{
				return _RemoveFeedGroupCommand
					?? (_RemoveFeedGroupCommand = new DelegateCommand(async () =>
					{
						if (await HohoemaApp.FeedManager.RemoveFeedGroup(FeedGroup))
						{
							PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
						}
					}));
			}
		}


		public ReactiveCommand RenameApplyCommand { get; private set; }

		private DelegateCommand _OpenFeedVideoListPageCommand;
		public DelegateCommand OpenFeedVideoListPageCommand
		{
			get
			{
				return _OpenFeedVideoListPageCommand
					?? (_OpenFeedVideoListPageCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.FeedVideoList, FeedGroup.Id);
					}));
			}
		}

		public ReactiveProperty<bool> CanUseFeedSource { get; private set; }

		internal void RemoveFeedSrouce(FeedItemSourceListItem feedSourceListItem)
		{
			var feedSource = feedSourceListItem.FeedSource;
			FeedGroup.RemoveUserFeedSource(feedSource);

			switch (feedSource.FollowItemType)
			{
				case FollowItemType.Tag:
					if (TagFeedSources.Remove(feedSourceListItem))
					{
						var favInfo = HohoemaApp.FollowManager.Tag.FollowInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
						if (favInfo != null)
						{
							TagFavItems.Add(favInfo);
						}
					}
					break;
				case FollowItemType.Mylist:
					if (MylistFeedSources.Remove(feedSourceListItem))
					{
						var favInfo = HohoemaApp.FollowManager.Mylist.FollowInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
						if (favInfo != null)
						{
							MylistFavItems.Add(favInfo);
						}
					}
					break;
				case FollowItemType.User:
					if (UserFeedSources.Remove(feedSourceListItem))
					{
						var favInfo = HohoemaApp.FollowManager.User.FollowInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
						if (favInfo != null)
						{
							UserFavItems.Add(favInfo);
						}
					}
					break;
				default:
					break;
			}

		}


		private DelegateCommand _AddTagFeedSourceCommand;
		public DelegateCommand AddTagFeedSourceCommand
		{
			get
			{
				return _AddTagFeedSourceCommand
					?? (_AddTagFeedSourceCommand = new DelegateCommand(async () =>
					{
						/// 
						var defaultSet = new Views.Service.ContentSelectDialogDefaultSet()
						{
							DialogTitle = "タグからフィード元を選択",
							ChoiceListTitle = "お気に入りタグから選ぶ",
							ChoiceList = HohoemaApp.FollowManager.Tag.FollowInfoItems.Select(x =>
								new Views.Service.SelectDialogPayload()
								{
									Label = x.Name,
									Id = x.Id
								}
							).ToList(),
							TextInputTitle = "タグを直接入力",
							GenerateCandiateList = null
						};

						var result = await ContentSelectDialogService.ShowDialog(defaultSet);

						if (result != null)
						{
							var item = FeedGroup.AddTagFeedSource(result.Id);

							TagFeedSources.Add(new FeedItemSourceListItem(item, this));

							System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にタグ「{result.Id}」を追加");

							await HohoemaApp.FeedManager.SaveOne(FeedGroup);
						}
					}));
			}
		}


		private DelegateCommand _AddMylistFeedSourceCommand;
		public DelegateCommand AddMylistFeedSourceCommand
		{
			get
			{
				return _AddMylistFeedSourceCommand
					?? (_AddMylistFeedSourceCommand = new DelegateCommand(async () =>
					{
						/// 
						var defaultSet = new Views.Service.ContentSelectDialogDefaultSet()
						{
							DialogTitle = "マイリストのフィード元を選択",
							ChoiceListTitle = "お気に入りマイリストから選ぶ",
							ChoiceList = HohoemaApp.FollowManager.Mylist.FollowInfoItems.Select(x =>
								new Views.Service.SelectDialogPayload()
								{
									Label = x.Name,
									Id = x.Id
								}
							).ToList(),
							TextInputTitle = "マイリストIDまたはキーワード",
							GenerateCandiateList = GenerateMylistCandidateList
						};

						var result = await ContentSelectDialogService.ShowDialog(defaultSet);

						if (result != null)
						{
							var item = FeedGroup.AddMylistFeedSource(result.Label, result.Id);

							MylistFeedSources.Add(new FeedItemSourceListItem(item, this));

							System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にマイリスト「{result.Label}({result.Id})」を追加");

							await HohoemaApp.FeedManager.SaveOne(FeedGroup);
						}
					}));
			}
		}

		private async Task<List<Views.Service.SelectDialogPayload>> GenerateMylistCandidateList(string word)
		{
			var list = new List<Views.Service.SelectDialogPayload>();
			// word is numbers
			int maybeMylistId;
			if (int.TryParse(word, out maybeMylistId))
			{
				// マイリストが実在するかチェック
				var mylistId = word;
				try
				{
					var mylistDetail = await HohoemaApp.ContentFinder.GetMylistGroupDetail(mylistId);
					if (mylistDetail != null)
					{
						var result = new Views.Service.SelectDialogPayload()
						{
							Id = mylistId,
							Label = mylistDetail.MylistGroup.Name
						};

						list.Add(result);
					}
				}
				catch { }
			}
			// word is text
			else
			{
				// マイリスト検索の上位10件を取得
				var searchResult = await HohoemaApp.NiconicoContext.Search.MylistSearchAsync(word, limit: 10);
				if (searchResult != null && searchResult.IsOK && searchResult.MylistGroupItems != null)
				{
					foreach (var searchItem in searchResult.MylistGroupItems)
					{
						list.Add(new Views.Service.SelectDialogPayload()
						{
							Id = searchItem.Id,
							Label = searchItem.Name
						});
					}
				}
			}


			return list;
		}


		private DelegateCommand _AddUserFeedSourceCommand;
		public DelegateCommand AddUserFeedSourceCommand
		{
			get
			{
				return _AddUserFeedSourceCommand
					?? (_AddUserFeedSourceCommand = new DelegateCommand(async () =>
					{
						/// 
						var defaultSet = new Views.Service.ContentSelectDialogDefaultSet()
						{
							DialogTitle = "ユーザー投稿動画のフィード元を選択",
							ChoiceListTitle = "お気に入りユーザーから選ぶ",
							ChoiceList = HohoemaApp.FollowManager.User.FollowInfoItems.Select(x =>
								new Views.Service.SelectDialogPayload()
								{
									Label = x.Name,
									Id = x.Id
								}
							).ToList(),
							TextInputTitle = "ユーザーIDを入力",
							GenerateCandiateList = GenerateUserCandidateList
						};

						var result = await ContentSelectDialogService.ShowDialog(defaultSet);

						if (result != null)
						{
							var item = FeedGroup.AddUserFeedSource(result.Label, result.Id);

							UserFeedSources.Add(new FeedItemSourceListItem(item, this));

							System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にユーザー「{result.Label}({result.Id})」を追加");

							await HohoemaApp.FeedManager.SaveOne(FeedGroup);
						}
					}));
			}
		}

		private async Task<List<Views.Service.SelectDialogPayload>> GenerateUserCandidateList(string word)
		{
			var list = new List<Views.Service.SelectDialogPayload>();
			// word is numbers
			int maybeMylistId;
			if (int.TryParse(word, out maybeMylistId))
			{
				// ユーザーが存在するかチェック
				var userId = word;
				try
				{
					var userInfo= await HohoemaApp.ContentFinder.GetUserInfo(userId);
					if (userInfo != null)
					{
						var result = new Views.Service.SelectDialogPayload()
						{
							Id = userId,
							Label = userInfo.Nickname
						};

						list.Add(result);
					}
				}
				catch { }
			}
			// word is text
			else
			{
				// TODO: ユーザーのローカルDBから選択候補ユーザーの検索
				/*
				Models.Db.UserInfoDb.
				var searchResult = await HohoemaApp.NiconicoContext.Search.MylistSearchAsync(word, limit: 10);
				if (searchResult != null && searchResult.IsOK && searchResult.MylistGroupItems != null)
				{
					foreach (var searchItem in searchResult.MylistGroupItems)
					{
						list.Add(new Views.Service.SelectDialogPayload()
						{
							Id = searchItem.Id,
							Label = searchItem.Name
						});
					}
				}
				*/
			}


			return list;
		}

	}

	public class FeedItemSourceListItem
	{
		public FeedGroupPageViewModel FeedGroupPageVM { get; private set; }
		public IFeedSource FeedSource { get; private set; }

		public string Name { get; private set; }

		public FeedItemSourceListItem(IFeedSource source, FeedGroupPageViewModel feedGroupPageVM)
		{
			FeedSource = source;
			FeedGroupPageVM = feedGroupPageVM;

			Name = FeedSource.Name;
		}

		private DelegateCommand _RemoveFeedSourceCommand;
		public DelegateCommand RemoveFeedSourceCommand
		{
			get
			{
				return _RemoveFeedSourceCommand
					?? (_RemoveFeedSourceCommand = new DelegateCommand(() =>
					{
						FeedGroupPageVM.RemoveFeedSrouce(this);
						
					}));
			}
		}
	}
}

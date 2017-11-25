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
		public Database.Feed FeedGroup { get; private set; }

		public ReactiveProperty<string> FeedGroupName { get; private set; }

		public ReactiveProperty<bool> IsDeleted { get; private set; }

		public ObservableCollection<FeedSourceBookmark> FeedSources { get; private set; }

		public ReactiveProperty<Database.BookmarkType> FavItemType { get; private set; }
		public ReactiveProperty<string> FeedSourceId { get; private set; }
		public ReactiveProperty<string> FeedSourceItemName { get; private set; }
		public ReactiveProperty<bool> ExistFeedSource { get; private set; }
		public ReactiveProperty<bool> IsPublicFeedSource { get; private set; }
		

		public Services.HohoemaDialogService HohoemaDialogService { get; private set; }

		public FeedGroupPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Services.HohoemaDialogService dialogService) 
			: base(hohoemaApp, pageManager)
		{
			HohoemaDialogService = dialogService;

			IsDeleted = new ReactiveProperty<bool>();

			FeedGroupName = new ReactiveProperty<string>();
			FeedSources = new ObservableCollection<FeedSourceBookmark>();

			FavItemType = new ReactiveProperty<Database.BookmarkType>();
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

			

			FeedSourceId.ToUnit()
				.Subscribe(_ => 
				{
					ExistFeedSource.Value = false;
					FeedSourceItemName.Value = "";
				});

            RenameApplyCommand = new ReactiveCommand();

			RenameApplyCommand.Subscribe(_ => 
			{
                FeedGroup.Label = FeedGroupName.Value;

                HohoemaApp.FeedManager.UpdateFeedGroup(FeedGroup);
			});
		}




		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			FeedGroup = null;

			if (e.Parameter is int)
			{
                var feedGroupId = (int)e.Parameter;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}
            else if (e.Parameter is string)
            {
                if (int.TryParse(e.Parameter as string, out int feedGroupId))
                {
                    FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
                }
            }

			IsDeleted.Value = FeedGroup == null;

			if (FeedGroup != null)
			{
				UpdateTitle($"『{FeedGroup.Label}』のフィード管理");

				FeedGroupName.Value = FeedGroup.Label;

				FeedSources.Clear();
				foreach (var mylistFeedSrouce in FeedGroup.Sources)
				{
					FeedSources.Add(new FeedSourceBookmark() { Feed = FeedGroup, Bookmark = mylistFeedSrouce });
				}

                HohoemaApp.FeedManager.FeedSourceRemoved += FeedManager_FeedSourceRemoved;
			}
		}


        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            HohoemaApp.FeedManager.FeedSourceRemoved -= FeedManager_FeedSourceRemoved;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
        private void FeedManager_FeedSourceRemoved(object sender, FeedSourceRemovedEventArgs e)
        {
            if (e.Feed.Id == FeedGroup?.Id)
            {
                var item = FeedSources.FirstOrDefault(x => x.Bookmark.Id == e.Bookmark.Id);
                FeedSources.Remove(item);
            }
        }

        public ReactiveCommand AddFeedCommand { get; private set; }

		private DelegateCommand _RemoveFeedGroupCommand;
		public DelegateCommand RemoveFeedGroupCommand
		{
			get
			{
				return _RemoveFeedGroupCommand
					?? (_RemoveFeedGroupCommand = new DelegateCommand(() =>
					{
						if (HohoemaApp.FeedManager.RemoveFeedGroup(FeedGroup))
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


        private DelegateCommand<Database.Bookmark> _OpenFeedSourcePageCommand;
        public DelegateCommand<Database.Bookmark> OpenFeedSourcePageCommand
        {
            get
            {
                return _OpenFeedSourcePageCommand
                    ?? (_OpenFeedSourcePageCommand = new DelegateCommand<Database.Bookmark>((item) =>
                    {
                        switch (item.BookmarkType)
                        {
                            case Database.BookmarkType.SearchWithTag:
                                var searchPayload = SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Tag, item.Content);
                                PageManager.OpenPage(HohoemaPageType.SearchResultTag, searchPayload.ToParameterString());
                                break;
                            case Database.BookmarkType.Mylist:
                                PageManager.OpenPage(HohoemaPageType.Mylist,
                                    new MylistPagePayload(item.Content).ToParameterString()
                                    );
                                break;
                            case Database.BookmarkType.User:
                                PageManager.OpenPage(HohoemaPageType.UserVideo, item.Content);
                                break;
                            default:
                                break;
                        }
                        
                    }));
            }
        }


        private DelegateCommand<FeedSourceBookmark> _RemoveFeedSourceCommand;
        public DelegateCommand<FeedSourceBookmark> RemoveFeedSourceCommand
        {
            get
            {
                return _RemoveFeedSourceCommand
                    ?? (_RemoveFeedSourceCommand = new DelegateCommand<FeedSourceBookmark>((item) =>
                    {
                        FeedGroup.Sources.Remove(item.Bookmark);

                        FeedSources.Remove(item);

                        HohoemaApp.FeedManager.UpdateFeedGroup(FeedGroup);
                    }));
            }
        }

        public ReactiveProperty<bool> CanUseFeedSource { get; private set; }

		

        private DelegateCommand _AddFeedSourceCommand;
        public DelegateCommand AddFeedSourceCommand
        {
            get
            {
                return _AddFeedSourceCommand
                    ?? (_AddFeedSourceCommand = new DelegateCommand(async () =>
                    {
                        var tagBookmarks = Database.BookmarkDb.GetAll(Database.BookmarkType.SearchWithTag);
                        var keywordBookmarks = Database.BookmarkDb.GetAll(Database.BookmarkType.SearchWithKeyword);
                        var userBookmarks = Database.BookmarkDb.GetAll(Database.BookmarkType.User);
                        var mylistBookmarks = Database.BookmarkDb.GetAll(Database.BookmarkType.Mylist);

                        var selectableContents = new List<Dialogs.ISelectableContainer>()
                        {
                            new Dialogs.ChoiceFromListSelectableContainer("タグ", tagBookmarks.Select(x => new Dialogs.SelectDialogPayload()
                            {
                                Label = x.Label,
                                Context = x
                            }))
                        };

                        var result = await HohoemaDialogService.ShowContentSelectDialogAsync("フィードソースを選択", selectableContents);

                        if (result != null)
                        {
                            var item = new Database.Bookmark()
                            {
                                Content = result.Id,
                                Label = result.Label,
                                BookmarkType = Database.BookmarkType.SearchWithTag,
                            };

                            FeedSources.Add(new FeedSourceBookmark() { Feed = FeedGroup, Bookmark = item });

                            System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にタグ「{result.Id}」を追加");

                            HohoemaApp.FeedManager.UpdateFeedGroup(FeedGroup);
                        }
                    }));
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
						var defaultSet = new Dialogs.ContentSelectDialogDefaultSet()
						{
							DialogTitle = "タグからフィード元を選択",
							ChoiceListTitle = "お気に入りタグから選ぶ",
							ChoiceList = HohoemaApp.FollowManager.Tag.FollowInfoItems.Select(x =>
								new Dialogs.SelectDialogPayload()
								{
									Label = x.Name,
									Id = x.Id
								}
							).ToList(),
							TextInputTitle = "タグを直接入力",
							GenerateCandiateList = null
						};

						var result = await HohoemaDialogService.ShowContentSelectDialogAsync(defaultSet);

						if (result != null)
						{
                            var item = new Database.Bookmark()
                            {
                                Content = result.Id,
                                Label = result.Label,
                                BookmarkType = Database.BookmarkType.SearchWithTag,
                            };

                            FeedSources.Add(new FeedSourceBookmark() { Feed = FeedGroup, Bookmark = item });

                            System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にタグ「{result.Id}」を追加");

                            HohoemaApp.FeedManager.UpdateFeedGroup(FeedGroup);
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
						var defaultSet = new Dialogs.ContentSelectDialogDefaultSet()
						{
							DialogTitle = "マイリストのフィード元を選択",
							ChoiceListTitle = "お気に入りマイリストから選ぶ",
							ChoiceList = HohoemaApp.FollowManager.Mylist.FollowInfoItems.Select(x =>
								new Dialogs.SelectDialogPayload()
								{
									Label = x.Name,
									Id = x.Id
								}
							).ToList(),
							TextInputTitle = "マイリストID またはキーワード",
							GenerateCandiateList = GenerateMylistCandidateList
						};

						var result = await HohoemaDialogService.ShowContentSelectDialogAsync(defaultSet);

						if (result != null)
						{
                            var item = new Database.Bookmark()
                            {
                                Content = result.Id,
                                Label = result.Label,
                                BookmarkType = Database.BookmarkType.Mylist,
                            };

                            FeedGroup.Sources.Add(item);

                            FeedSources.Add(new FeedSourceBookmark() { Feed = FeedGroup, Bookmark = item });

                            System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にマイリスト「{result.Label}({result.Id})」を追加");

                            HohoemaApp.FeedManager.UpdateFeedGroup(FeedGroup);
                        }
					}));
			}
		}

		private async Task<List<Dialogs.SelectDialogPayload>> GenerateMylistCandidateList(string word)
		{
			var list = new List<Dialogs.SelectDialogPayload>();
			// word is numbers
			int maybeMylistId;
			if (int.TryParse(word, out maybeMylistId))
			{
				// マイリストが実在するかチェック
				var mylistId = word;
				try
				{
					var mylistDetail = await HohoemaApp.ContentProvider.GetMylistGroupDetail(mylistId);
					if (mylistDetail != null)
					{
						var result = new Dialogs.SelectDialogPayload()
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
						list.Add(new Dialogs.SelectDialogPayload()
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
						var defaultSet = new Dialogs.ContentSelectDialogDefaultSet()
						{
							DialogTitle = "ユーザー投稿動画のフィード元を選択",
							ChoiceListTitle = "お気に入りユーザーから選ぶ",
							ChoiceList = HohoemaApp.FollowManager.User.FollowInfoItems.Select(x =>
								new Dialogs.SelectDialogPayload()
								{
									Label = x.Name,
									Id = x.Id
								}
							).ToList(),
							TextInputTitle = "ユーザーIDを入力",
							GenerateCandiateList = GenerateUserCandidateList
						};

						var result = await HohoemaDialogService.ShowContentSelectDialogAsync(defaultSet);

						if (result != null)
						{
							var item = new Database.Bookmark()
                            {
                                Content = result.Id,
                                Label = result.Label,
                                BookmarkType = Database.BookmarkType.User,
                            };

                            FeedGroup.Sources.Add(item);

                            FeedSources.Add(new FeedSourceBookmark() { Feed = FeedGroup, Bookmark = item });

                            System.Diagnostics.Debug.WriteLine($"{FeedGroup.Label} にユーザー「{result.Label}({result.Id})」を追加");

                            HohoemaApp.FeedManager.UpdateFeedGroup(FeedGroup);
                        }
					}));
			}
		}

		private async Task<List<Dialogs.SelectDialogPayload>> GenerateUserCandidateList(string word)
		{
			var list = new List<Dialogs.SelectDialogPayload>();
			// word is numbers
			int maybeMylistId;
			if (int.TryParse(word, out maybeMylistId))
			{
				// ユーザーが存在するかチェック
				var userId = word;
				try
				{
					var userInfo= await HohoemaApp.ContentProvider.GetUserInfo(userId);
					if (userInfo != null)
					{
						var result = new Dialogs.SelectDialogPayload()
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


    public class FeedSourceBookmark
    {
        public Database.Feed Feed { get; set; }
        public Database.Bookmark Bookmark { get; set; }
    }

}

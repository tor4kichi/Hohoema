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
		public FeedGroup FeedGroup { get; private set; }

		public ReactiveProperty<string> FeedGroupName { get; private set; }

		public ReactiveProperty<bool> IsDeleted { get; private set; }

		public ObservableCollection<FeedItemSourceListItem> MylistFeedSources { get; private set; }
		public ObservableCollection<FeedItemSourceListItem> TagFeedSources { get; private set; }
		public ObservableCollection<FeedItemSourceListItem> UserFeedSources { get; private set; }

		public ReadOnlyReactiveProperty<bool> HasMylistFeedSource { get; private set; }
		public ReadOnlyReactiveProperty<bool> HasTagFeedSource { get; private set; }
		public ReadOnlyReactiveProperty<bool> HasUserFeedSource { get; private set; }


		public ReactiveProperty<bool> SelectFromFavItems { get; private set; }


		public ReactiveProperty<FavInfo> SelectedFavInfo { get; private set; }

		public ObservableCollection<FavInfo> MylistFavItems { get; private set; }
		public ObservableCollection<FavInfo> TagFavItems { get; private set; }
		public ObservableCollection<FavInfo> UserFavItems { get; private set; }

		public ReactiveProperty<FavoriteItemType> FavItemType { get; private set; }
		public ReactiveProperty<string> FeedSourceId { get; private set; }
		public ReactiveProperty<bool> ExistFeedSource { get; private set; }
		public ReactiveProperty<bool> IsPublicFeedSource { get; private set; }
		
		private string _FeedSourceName;

		public FeedGroupPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true )
		{
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


			MylistFavItems = new ObservableCollection<FavInfo>();
			TagFavItems = new ObservableCollection<FavInfo>();
			UserFavItems = new ObservableCollection<FavInfo>();

			SelectFromFavItems = new ReactiveProperty<bool>(true);
			SelectedFavInfo = new ReactiveProperty<FavInfo>();

			FavItemType = new ReactiveProperty<FavoriteItemType>();
			FeedSourceId = new ReactiveProperty<string>();
			ExistFeedSource = new ReactiveProperty<bool>();
			IsPublicFeedSource = new ReactiveProperty<bool>();

			FavItemType.Subscribe(x => 
			{
				FeedSourceId.Value = "";
				ExistFeedSource.Value = false;
				
				_FeedSourceName = "";


				// お気に入りアイテムがある場合は、「お気に入りから選択」をデフォルトに
				switch (x)
				{
					case FavoriteItemType.Tag:
						SelectFromFavItems.Value = TagFavItems.Count > 0;
						break;
					case FavoriteItemType.Mylist:
						SelectFromFavItems.Value = MylistFavItems.Count > 0;
						break;
					case FavoriteItemType.User:
						SelectFromFavItems.Value = UserFavItems.Count > 0;
						break;
					default:
						break;
				}
				

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
						return;
					}

					ExistFeedSource.Value = false;

					if (FavItemType.Value == FavoriteItemType.Tag)
					{
						ExistFeedSource.Value = !string.IsNullOrWhiteSpace(FeedSourceId.Value);
						IsPublicFeedSource.Value = true;
						_FeedSourceName = FeedSourceId.Value;
					}
					else
					{
						if (string.IsNullOrWhiteSpace(FeedSourceId.Value))
						{
							ExistFeedSource.Value = false;
						}
						else
						{
							if (FavItemType.Value == FavoriteItemType.Mylist)
							{
								try
								{
									var mylistRes = await HohoemaApp.ContentFinder.GetMylist(FeedSourceId.Value);
									var mylist = mylistRes?.Mylistgroup.ElementAtOrDefault(0);
									
									if (mylist != null)
									{
										ExistFeedSource.Value = true;
										IsPublicFeedSource.Value = mylist.IsPublic;
										_FeedSourceName = mylist.Name;
									}
								}
								catch
								{
									ExistFeedSource.Value = false;
								}

							}
							else if (FavItemType.Value == FavoriteItemType.User)
							{
								try
								{
									var user = await HohoemaApp.ContentFinder.GetUserDetail(FeedSourceId.Value);
									if (user != null)
									{
										ExistFeedSource.Value = true;
										IsPublicFeedSource.Value = !user.IsOwnerVideoPrivate;
										_FeedSourceName = user.Nickname;
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
								_FeedSourceName = "";
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

					if (favInfo.FavoriteItemType != FavItemType.Value)
					{
						throw new Exception();
					}
				}
				else
				{
					// idからMylistGroupを引く
					// 公開されていない場合にはエラー
					name = _FeedSourceName;
					id = FeedSourceId.Value;
				}

				var favManager = HohoemaApp.FavManager;
				var feedManager = HohoemaApp.FeedManager;
				IFeedSource feedSource;
				switch (FavItemType.Value)
				{
					case FavoriteItemType.Tag:

						feedSource = FeedGroup.AddTagFeedSource(id);
						if (feedSource != null)
						{
							var favInfo = favManager.Tag.FavInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								TagFavItems.Remove(favInfo);
							}

							TagFeedSources.Add(new FeedItemSourceListItem(feedSource, this));
						}

						break;
					case FavoriteItemType.Mylist:

						feedSource = FeedGroup.AddMylistFeedSource(name, id);
						if (feedSource != null)
						{
							var favInfo = favManager.Mylist.FavInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								MylistFavItems.Remove(favInfo);
							}

							MylistFeedSources.Add(new FeedItemSourceListItem(feedSource, this));
						}

						break;
					case FavoriteItemType.User:

						feedSource = FeedGroup.AddUserFeedSource(name, id);
						if (feedSource != null)
						{
							var favInfo = favManager.User.FavInfoItems.SingleOrDefault(x => x.Id == id);
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

			IsDeleted.Value = FeedGroup == null;

			if (FeedGroup != null)
			{
				UpdateTitle(FeedGroup.Label);

				FeedGroupName.Value = FeedGroup.Label;

				MylistFeedSources.Clear();
				foreach (var mylistFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FavoriteItemType == FavoriteItemType.Mylist))
				{
					MylistFeedSources.Add(new FeedItemSourceListItem(mylistFeedSrouce, this));
				}

				MylistFavItems.Clear();
				foreach (var mylistFavInfo in HohoemaApp.FavManager.Mylist.FavInfoItems.Where(x => MylistFeedSources.All(y => x.Id != y.FeedSource.Id)))
				{
					MylistFavItems.Add(mylistFavInfo);
				}

				TagFeedSources.Clear();
				foreach (var tagFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FavoriteItemType == FavoriteItemType.Tag))
				{
					TagFeedSources.Add(new FeedItemSourceListItem(tagFeedSrouce, this));
				}

				TagFavItems.Clear();
				foreach (var tagFavInfo in HohoemaApp.FavManager.Tag.FavInfoItems.Where(x => TagFeedSources.All(y => x.Id != y.FeedSource.Id)))
				{
					TagFavItems.Add(tagFavInfo);
				}

				UserFeedSources.Clear();
				foreach (var userFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FavoriteItemType == FavoriteItemType.User))
				{
					UserFeedSources.Add(new FeedItemSourceListItem(userFeedSrouce, this));
				}

				UserFavItems.Clear();
				foreach (var userFavInfo in HohoemaApp.FavManager.User.FavInfoItems.Where(x => UserFeedSources.All(y => x.Id != y.FeedSource.Id)))
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
							if (PageManager.NavigationService.CanGoBack())
							{
								PageManager.NavigationService.GoBack();
							}
							else
							{
								PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
							}
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

		internal void RemoveFeedSrouce(FeedItemSourceListItem feedSourceListItem)
		{
			var feedSource = feedSourceListItem.FeedSource;
			FeedGroup.RemoveUserFeedSource(feedSource);

			switch (feedSource.FavoriteItemType)
			{
				case FavoriteItemType.Tag:
					if (TagFeedSources.Remove(feedSourceListItem))
					{
						var favInfo = HohoemaApp.FavManager.Tag.FavInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
						if (favInfo != null)
						{
							TagFavItems.Add(favInfo);
						}
					}
					break;
				case FavoriteItemType.Mylist:
					if (MylistFeedSources.Remove(feedSourceListItem))
					{
						var favInfo = HohoemaApp.FavManager.Mylist.FavInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
						if (favInfo != null)
						{
							MylistFavItems.Add(favInfo);
						}
					}
					break;
				case FavoriteItemType.User:
					if (UserFeedSources.Remove(feedSourceListItem))
					{
						var favInfo = HohoemaApp.FavManager.User.FavInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
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

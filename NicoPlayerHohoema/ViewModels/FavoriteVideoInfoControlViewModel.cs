using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public FeedVideoInfoControlViewModel(FavFeedItem feedItem, FeedGroup feedGroup, NicoVideo nicoVideo, PageManager pageMan)
			: base(nicoVideo, pageMan)
		{
			IsUnread = feedItem.ToReactivePropertyAsSynchronized(x => x.IsUnread)
				.AddTo(_CompositeDisposable);

			_FafFeedItem = feedItem;
			_FeedGroup = feedGroup;
		}


		private DelegateCommand _OpenFeedSourceCommand;
		public DelegateCommand OpenFeedSourceCommand
		{
			get
			{
				return _OpenFeedSourceCommand
					?? (_OpenFeedSourceCommand = new DelegateCommand(() => 
					{
						/*
						var feedList = _FafFeedItem.ParentList;
						switch (SourceType)
						{
							case FavoriteItemType.Tag:
								PageManager.OpenPage(HohoemaPageType.Search, new SearchOption()
								{
									Keyword = feedList.Id,
									SearchTarget = SearchTarget.Tag,
									Sort = Mntone.Nico2.Sort.FirstRetrieve,
									Order = Mntone.Nico2.Order.Descending,
								}.ToParameterString());
								break;
							case FavoriteItemType.Mylist:
								PageManager.OpenPage(HohoemaPageType.UserMylist, feedList.Id);
								break;
							case FavoriteItemType.User:
								PageManager.OpenPage(HohoemaPageType.UserInfo, feedList.Id);
								break;
							default:
								break;
						}
						*/
					}));
			}
		}

		FavFeedItem _FafFeedItem;
		FeedGroup _FeedGroup;

		public ReactiveProperty<bool> IsUnread { get; private set; }
		public FavoriteItemType SourceType { get; private set; }
		public string SourceTitle { get; private set; }
	}

}

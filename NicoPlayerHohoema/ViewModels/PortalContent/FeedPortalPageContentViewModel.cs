using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class FeedPortalPageContentViewModel : PotalPageContentViewModel
	{
		public FeedPortalPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;

			FeedGroupItems = new ObservableCollection<FeedGroupPortalContentViewModel>();
		}

		protected override async void NavigateTo()
		{
			while (_HohoemaApp.FavManager == null)
			{
				await Task.Delay(100);
			}

			if (FeedGroupItems.Count == 0)
			{
				await UpdateUnreadFeedItems();
			}

			base.NavigateTo();
		}		


		private async Task UpdateUnreadFeedItems()
		{
			foreach (var groups in FeedGroupItems)
			{
				groups.Dispose();
			}

			FeedGroupItems.Clear();


			foreach (var group in _HohoemaApp.FeedManager.FeedGroups)
			{
				var groupVM = new FeedGroupPortalContentViewModel(group, _HohoemaApp, PageManager);

				FeedGroupItems.Add(groupVM);

				await groupVM.Initialize();
			}
		}

		private DelegateCommand _OpenFavFeedListCommand;
		public DelegateCommand OpenFavFeedListCommand
		{
			get
			{
				return _OpenFavFeedListCommand
					?? (_OpenFavFeedListCommand = new DelegateCommand(() =>
					{
//						PageManager.OpenPage(HohoemaPageType.FavoriteAllFeed);
					}));
			}
		}


		public ObservableCollection<FeedGroupPortalContentViewModel> FeedGroupItems { get; private set; }


		HohoemaApp _HohoemaApp;
	}

	public class FeedGroupPortalContentViewModel : BindableBase, IDisposable
	{
		public FeedGroup FeedGroup { get; private set; }
		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		public string Name { get; private set; }

		public ObservableCollection<FeedVideoInfoControlViewModel> FeedItems { get; private set; }

		public FeedGroupPortalContentViewModel(FeedGroup feedGroup, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			FeedGroup = feedGroup;
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			FeedItems = new ObservableCollection<FeedVideoInfoControlViewModel>();

			Name = FeedGroup.Label;
		}


		public async Task Initialize()
		{
			// TODO: FeedGroupのバックグラウンドの更新を待ってから表示したい

			while(FeedGroup.IsNeedRefresh)
			{
				await Task.Delay(100);
			}

			foreach (var feed in FeedGroup.FeedItems.Where(x => x.IsUnread).Take(5))
			{
				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(feed.VideoId);
				FeedItems.Add(new FeedVideoInfoControlViewModel(feed, FeedGroup, nicoVideo, PageManager));
			}
		}

		public void Dispose()
		{
			foreach (var item in FeedItems)
			{
				item.Dispose();
			}

			FeedItems.Clear();
		}


		private DelegateCommand _OpenFeedGroupCommand;
		public DelegateCommand OpenFeedGroupCommand
		{
			get
			{
				return _OpenFeedGroupCommand
					?? (_OpenFeedGroupCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Label);
					}));
			}
		}

		private DelegateCommand _OpenFeedVideoListCommand;
		public DelegateCommand OpenFeedVideoListCommand
		{
			get
			{
				return _OpenFeedVideoListCommand
					?? (_OpenFeedVideoListCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.FeedVideoList, FeedGroup.Label);
					}));
			}
		}
	}
}

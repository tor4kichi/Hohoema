using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.Models;
using System.Diagnostics;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Prism.Commands;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class FavoriteAllFeedPageViewModel : HohoemaVideoListingPageViewModelBase<FavoriteVideoInfoControlViewModel>
	{
		public FavoriteAllFeedPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn: true)
		{
			AllMarkAsReadCommand = new DelegateCommand(async () =>
			{
				await HohoemaApp.FavFeedManager.MarkAsReadAllVideo();
			}
			, () =>
			{
				return HohoemaApp.FavFeedManager.GetUnreadFeedItems().Any(x => x.IsUnread);
			});


			SelectedItemsMarkAsReadCommand = SelectedVideoInfoItems.ToCollectionChanged()
				.Select(x => SelectedVideoInfoItems.Count > 0)
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			SelectedItemsMarkAsReadCommand.Subscribe(async _ =>
			{
				foreach (var item in SelectedVideoInfoItems)
				{
					await HohoemaApp.FavFeedManager.MarkAsRead(item.VideoId);
					await HohoemaApp.FavFeedManager.MarkAsRead(item.RawVideoId);
				}

				ClearSelection();
			})
			.AddTo(_CompositeDisposable);
		}

		protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			AllMarkAsReadCommand.RaiseCanExecuteChanged();

			return Task.CompletedTask;
		}


		protected override uint IncrementalLoadCount
		{
			get { return 20; }
		}

		
	

		protected override IIncrementalSource<FavoriteVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FavriteAllFeedIncrementalSource(HohoemaApp.FavFeedManager, HohoemaApp.MediaManager, PageManager);
		}



		public DelegateCommand AllMarkAsReadCommand { get; private set; }
		public ReactiveCommand SelectedItemsMarkAsReadCommand { get; private set; }

	}


	public class FavriteAllFeedIncrementalSource : IIncrementalSource<FavoriteVideoInfoControlViewModel>
	{
		FavFeedManager _FavFeedManager;
		NiconicoMediaManager _NiconicoMediaManager;
		PageManager _PageManager;

		public List<FavFeedItem> FeedItems { get; private set; }

		public FavriteAllFeedIncrementalSource(FavFeedManager favFeedManager, NiconicoMediaManager mediaManager, PageManager pageManager)
		{
			_FavFeedManager = favFeedManager;
			_NiconicoMediaManager = mediaManager;
			_PageManager = pageManager;
		}

		public async Task<IEnumerable<FavoriteVideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			if (FeedItems == null || pageIndex == 1)
			{
				await _FavFeedManager.UpdateAll();
				FeedItems = _FavFeedManager.GetAllFeedItems().Take(100).ToList();
			}

			var head = pageIndex - 1;
			var currentItems = FeedItems.Skip((int)head).Take((int)pageSize).ToList();

			var list = new List<FavoriteVideoInfoControlViewModel>();
			foreach (var feed in currentItems)
			{
				try
				{
					var nicoVideo = await _NiconicoMediaManager.GetNicoVideo(feed.VideoId);
					var vm = new FavoriteVideoInfoControlViewModel(feed, nicoVideo, _PageManager);

					list.Add(vm);

					vm.LoadThumbnail();
				}
				catch (Exception ex)
				{
					Debug.Fail("FeedListのアイテムのNicoVideoの取得に失敗しました。", ex.Message);
				}
			}

			return list;
		}
	}
}

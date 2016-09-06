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
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedVideoListPageViewModel : HohoemaVideoListingPageViewModelBase<FeedVideoInfoControlViewModel>
	{
		public FeedGroup FeedGroup { get; private set; }


		public FeedVideoListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService, isRequireSignIn: true)
		{
			AllMarkAsReadCommand = new DelegateCommand(async () =>
			{
				FeedGroup.ForceMarkAsRead();

				await HohoemaApp.FeedManager.SaveOne(FeedGroup);

				AllMarkAsReadCommand.RaiseCanExecuteChanged();
			}
			, () =>
			{
				return (FeedGroup?.GetUnreadItemCount() ?? 0 ) > 0;
			});


			SelectedItemsMarkAsReadCommand = SelectedItems.ToCollectionChanged()
				.Select(x => SelectedItems.Count(y => y.IsUnread.Value) > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			SelectedItemsMarkAsReadCommand.Subscribe(async _ =>
			{
				foreach (var item in SelectedItems)
				{
					await HohoemaApp.FeedManager.MarkAsRead(item.VideoId);
					await HohoemaApp.FeedManager.MarkAsRead(item.RawVideoId);
				}

				ClearSelection();
			})
			.AddTo(_CompositeDisposable);

			OpenFeedGroupPageCommand = new DelegateCommand(() => 
			{
				if (FeedGroup != null)
				{
					PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
				}
			});
		}

		protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is Guid)
			{
				var feedGroupId = (Guid)e.Parameter;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}

			if (FeedGroup == null)
			{
				// 削除済み？
				PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
			}
			else
			{
				UpdateTitle(FeedGroup.Label);

				AllMarkAsReadCommand.RaiseCanExecuteChanged();
			}

			return Task.CompletedTask;
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (FeedGroup?.IsNeedRefresh ?? false)
			{
				return true;
			}
			return base.CheckNeedUpdateOnNavigateTo(mode);
		}

		protected override uint IncrementalLoadCount
		{
			get { return 5; }
		}

		
	

		protected override IIncrementalSource<FeedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FeedVideoIncrementalSource(FeedGroup, HohoemaApp.FeedManager, HohoemaApp.MediaManager, PageManager);
		}


		public DelegateCommand AllMarkAsReadCommand { get; private set; }
		public ReactiveCommand SelectedItemsMarkAsReadCommand { get; private set; }

		public DelegateCommand OpenFeedGroupPageCommand { get; private set; }
	}


	public class FeedVideoIncrementalSource : IIncrementalSource<FeedVideoInfoControlViewModel>
	{
		HohoemaApp _HohoemaApp;
		FeedManager _FavFeedManager;
		NiconicoMediaManager _NiconicoMediaManager;
		PageManager _PageManager;
		FeedGroup _FeedGroup;


		public FeedVideoIncrementalSource(FeedGroup feedGroup, FeedManager favFeedManager, NiconicoMediaManager mediaManager, PageManager pageManager)
		{
			_HohoemaApp = feedGroup.HohoemaApp;
			_FeedGroup = feedGroup;
			_FavFeedManager = favFeedManager;
			_NiconicoMediaManager = mediaManager;
			_PageManager = pageManager;
		}

		public async Task<int> ResetSource()
		{
			await _FeedGroup.Refresh();

			await SchedulePreloading(0, 20);

			return _FeedGroup.FeedItems.Count;
		}

		private Task SchedulePreloading(int start, int count)
		{
			// 先頭20件を先行ロード
			return _FavFeedManager.HohoemaApp.ThumbnailBackgroundLoader.Schedule(
				new SimpleBackgroundUpdate("FeedGroup:" + _FeedGroup.Label + $" [{start} - {count}]"
				, () => UpdateItemsThumbnailInfo(start, count)
				)
				);
		}

		private async Task UpdateItemsThumbnailInfo(int start, int count)
		{
			foreach (var item in _FeedGroup.FeedItems.AsParallel().Skip(start).Take(count))
			{
				if (!_HohoemaApp.IsLoggedIn) { return; }

				await _HohoemaApp.MediaManager.EnsureNicoVideoObjectAsync(item.VideoId);
			}
		}

		public async Task<IEnumerable<FeedVideoInfoControlViewModel>> GetPagedItems(int head, int count)
		{
			var list = new List<FeedVideoInfoControlViewModel>();

			var currentItems = _FeedGroup.FeedItems.Skip(head).Take(count).ToList();

			foreach (var feed in currentItems)
			{
				try
				{
					var nicoVideo = await _NiconicoMediaManager.GetNicoVideoAsync(feed.VideoId);
					var vm = new FeedVideoInfoControlViewModel(feed, _FeedGroup, nicoVideo, _PageManager);

					list.Add(vm);
				}
				catch (Exception ex)
				{
					Debug.Fail("FeedListのアイテムのNicoVideoの取得に失敗しました。", ex.Message);
				}
			}

			await SchedulePreloading(head + count, count);

			return list;
		}
	}
}

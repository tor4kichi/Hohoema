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
		}

		protected override Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				var label = e.Parameter as string;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(label);
			}

			if (FeedGroup == null)
			{
				throw new Exception("");
			}


			UpdateTitle(FeedGroup.Label);

			AllMarkAsReadCommand.RaiseCanExecuteChanged();


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
			get { return 20; }
		}

		
	

		protected override IIncrementalSource<FeedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FeedVideoIncrementalSource(FeedGroup, HohoemaApp.FeedManager, HohoemaApp.MediaManager, PageManager);
		}


		public DelegateCommand AllMarkAsReadCommand { get; private set; }
		public ReactiveCommand SelectedItemsMarkAsReadCommand { get; private set; }

	}


	public class FeedVideoIncrementalSource : IIncrementalSource<FeedVideoInfoControlViewModel>
	{
		FeedManager _FavFeedManager;
		NiconicoMediaManager _NiconicoMediaManager;
		PageManager _PageManager;
		FeedGroup _FeedGroup;


		public FeedVideoIncrementalSource(FeedGroup feedGroup, FeedManager favFeedManager, NiconicoMediaManager mediaManager, PageManager pageManager)
		{
			_FeedGroup = feedGroup;
			_FavFeedManager = favFeedManager;
			_NiconicoMediaManager = mediaManager;
			_PageManager = pageManager;
		}

		public async Task<int> ResetSource()
		{
			await _FeedGroup.Refresh();

			return _FeedGroup.FeedItems.Count;
		}

		public async Task<IEnumerable<FeedVideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			var list = new List<FeedVideoInfoControlViewModel>();

			var head = pageIndex - 1;
			var currentItems = _FeedGroup.FeedItems.Skip((int)head).Take((int)pageSize).ToList();

			foreach (var feed in currentItems)
			{
				try
				{
					var nicoVideo = await _NiconicoMediaManager.GetNicoVideo(feed.VideoId);
					var vm = new FeedVideoInfoControlViewModel(feed, _FeedGroup, nicoVideo, _PageManager);

					list.Add(vm);

					await vm.LoadThumbnail();
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

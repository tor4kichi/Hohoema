using Mntone.Nico2;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Search;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NicoPlayerHohoema.ViewModels
{
	public class RankingCategoryPageViewModel : ViewModelBase
	{
		public RankingCategoryPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea, PageManager pageManager)
		{
			HohoemaApp = hohoemaApp;
			ContentFinder = HohoemaApp.ContentFinder;
			_EventAggregator = ea;
			_PageManager = pageManager;

			RankingSettings = hohoemaApp.UserSettings.RankingSettings;
			IsFailedRefreshRanking = new ReactiveProperty<bool>(false);
			CanChangeRankingParameter = new ReactiveProperty<bool>(false);

			// ランキングの対象
			RankingTargetItems = new List<RankingTargetListItem>()
			{
				new RankingTargetListItem(RankingTarget.view),
				new RankingTargetListItem(RankingTarget.res),
				new RankingTargetListItem(RankingTarget.mylist)
			};

			SelectedRankingTarget = new ReactiveProperty<RankingTargetListItem>(RankingTargetItems[0], ReactivePropertyMode.DistinctUntilChanged);


			// ランキングの集計期間
			RankingTimeSpanItems = new List<RankingTimeSpanListItem>()
			{
				new RankingTimeSpanListItem(RankingTimeSpan.hourly),
				new RankingTimeSpanListItem(RankingTimeSpan.daily),
				new RankingTimeSpanListItem(RankingTimeSpan.weekly),
				new RankingTimeSpanListItem(RankingTimeSpan.monthly),
				new RankingTimeSpanListItem(RankingTimeSpan.total),
			};

			SelectedRankingTimeSpan = new ReactiveProperty<RankingTimeSpanListItem>(RankingTimeSpanItems[0], ReactivePropertyMode.DistinctUntilChanged);


			Observable.CombineLatest(
				SelectedRankingTarget.ToUnit(),
				SelectedRankingTimeSpan.ToUnit()
				)
				.Throttle(TimeSpan.FromSeconds(0.25), UIDispatcherScheduler.Default)
				.SubscribeOnUIDispatcher()
				.Subscribe(x => 
				{
					RefreshRankingList();
				});
			
		}



		internal void ShowVideoInfomation(string videoUrl)
		{
			_PageManager.OpenPage(HohoemaPageType.VideoInfomation, videoUrl);
//			_EventAggregator.GetEvent<Events.PlayNicoVideoEvent>()
//				.Publish(videoUrl);
		}


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);


			RankingCategoryInfo categoryInfo = null;
			if (e.Parameter is string)
			{
				categoryInfo = RankingCategoryInfo.FromParameterString(e.Parameter as string);
			}
			else
			{
				return;
			}


			if (categoryInfo.Equals(CategoryInfo))
			{
				return;
			}

			CategoryInfo = categoryInfo;

			RefreshRankingList();

			// TODO: ページタイトルを変更したい

		}


		public void RefreshRankingList()
		{
			IsFailedRefreshRanking.Value = false;

			IIncrementalSource<RankedVideoInfoControlViewModel> source = null;
			uint pageSize = 20;
			try
			{
				switch (CategoryInfo.RankingSource)
				{
					case RankingSource.CategoryRanking:
						RankingCategory = (RankingCategory)Enum.Parse(typeof(RankingCategory), CategoryInfo.Parameter);
						var target = SelectedRankingTarget.Value.TargetType;
						var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
						source = new CategoryRankingLoadingSource(HohoemaApp, _PageManager, RankingCategory, target, timeSpan);

						CanChangeRankingParameter.Value = true;
						break;
					case RankingSource.SearchWithMostPopular:

						source = new CustomRankingLoadingSource(HohoemaApp, _PageManager, CategoryInfo.Parameter);
						pageSize = 30;

						CanChangeRankingParameter.Value = false;
						break;
					default:
						throw new NotImplementedException();
				}
			}
			catch
			{
				IsFailedRefreshRanking.Value = true;
			}


			RankingItems = new IncrementalLoadingCollection<IIncrementalSource<RankedVideoInfoControlViewModel>, RankedVideoInfoControlViewModel>(source, pageSize);
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			RankingSettings.Save();

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }

		public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }



		public RankingCategoryInfo CategoryInfo { get; private set; }

		public RankingCategory RankingCategory { get; private set; }

		public List<RankingTargetListItem> RankingTargetItems { get; private set; }
		public ReactiveProperty<RankingTargetListItem> SelectedRankingTarget { get; private set; }

		public List<RankingTimeSpanListItem> RankingTimeSpanItems { get; private set; }
		public ReactiveProperty<RankingTimeSpanListItem> SelectedRankingTimeSpan { get; private set; }

		public IncrementalLoadingCollection<IIncrementalSource<RankedVideoInfoControlViewModel>, RankedVideoInfoControlViewModel> RankingItems { get; private set; }

		private PageManager _PageManager;
		private HohoemaApp HohoemaApp;
		private NiconicoContentFinder ContentFinder;
		private EventAggregator _EventAggregator;
		public RankingSettings RankingSettings { get; private set; }
	}


	public class CategoryRankingLoadingSource : IIncrementalSource<RankedVideoInfoControlViewModel>
	{
		public CategoryRankingLoadingSource(HohoemaApp app, PageManager pageManager, RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
			_Category = category;
			_Target = target;
			_TimeSpan = timeSpan;
		}


		public Task<IEnumerable<RankedVideoInfoControlViewModel>> GetPagedItems(uint position, uint pageSize)
		{
			return AsyncInfo.Run(async (token) => 
			{
				var contentFinder = _HohoemaApp.ContentFinder;
				var mediaManager = _HohoemaApp.MediaManager;

				token.ThrowIfCancellationRequested();

				if (RankingRss == null)
				{
					RankingRss = await NiconicoRanking.GetRankingData(_Target, _TimeSpan, _Category);
				}

				token.ThrowIfCancellationRequested();


				var head = (int)(position);
				var tail = head + pageSize;

				List<RankedVideoInfoControlViewModel> items = new List<RankedVideoInfoControlViewModel>();
				for (int i = head; i < tail; ++i)
				{
					token.ThrowIfCancellationRequested();

					var rank = i;

					if (rank >= RankingRss.Channel.Items.Count)
					{
						break;
					}

					var item = RankingRss.Channel.Items[rank-1];
					var nicoVideo = await mediaManager.GetNicoVideo(item.GetVideoId());


					var vm = new RankedVideoInfoControlViewModel(
						(uint)(rank)
						, nicoVideo
						, _PageManager
					);
					vm.LoadThumbnail();

					items.Add(vm);
				}

				token.ThrowIfCancellationRequested();

				return items.AsEnumerable();
			})
			.AsTask();			
		}


		NiconicoRankingRss RankingRss;
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		RankingCategory _Category;
		RankingTarget _Target;
		RankingTimeSpan _TimeSpan;
	}



	public class CustomRankingLoadingSource : IIncrementalSource<RankedVideoInfoControlViewModel>
	{
		public CustomRankingLoadingSource(HohoemaApp app, PageManager pageManager, string parameter)
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
			_Parameter = parameter;
		}


		public async Task<IEnumerable<RankedVideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			// 
			var contentFinder = _HohoemaApp.ContentFinder;
			var mediaManager = _HohoemaApp.MediaManager;


			var res = await contentFinder.GetKeywordSearch(_Parameter, pageIndex + 1, SortMethod.Popurarity);			

			var head = pageIndex * pageSize;

			List<RankedVideoInfoControlViewModel> items = new List<RankedVideoInfoControlViewModel>();

			for (int i = 0; i < res.list.Count; ++i)
			{
				var item = res.list[i];
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.id);

				var videoInfoVM = new RankedVideoInfoControlViewModel(
					(uint)(i + 1)
					, nicoVideo
					, _PageManager
					);

				videoInfoVM.LoadThumbnail();

				items.Add(videoInfoVM);
			}

			return items;
		}

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		string _Parameter;

	}





	public class RankedVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public RankedVideoInfoControlViewModel(uint rank, NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			Rank = rank;
		}



		public uint Rank { get; private set; }
	}

	public class RankingTargetListItem : BindableBase
	{
		public RankingTargetListItem(RankingTarget target)
		{
			TargetType = target;
			Label = target.ToCultulizedText();
		}

		public string Label { get; private set; }

		public RankingTarget TargetType { get; private set; }
	}


	public class RankingTimeSpanListItem : BindableBase
	{
		public RankingTimeSpanListItem(RankingTimeSpan rankingTimeSpan)
		{
			TimeSpan = rankingTimeSpan;
			Label = rankingTimeSpan.ToCultulizedText();
		}

		public string Label { get; private set; }

		public RankingTimeSpan TimeSpan { get; private set; }
	}



	public class RankingCategoryListItem : SelectableItem<RankingCategoryInfo>
	{
		public RankingCategoryListItem(RankingCategoryInfo categoryInfo, Action<RankingCategoryInfo> selectedAction)
			: base(categoryInfo, selectedAction)
		{
			Label = categoryInfo.DisplayLabel;
		}

		public string Label { get; private set; }
	}

}

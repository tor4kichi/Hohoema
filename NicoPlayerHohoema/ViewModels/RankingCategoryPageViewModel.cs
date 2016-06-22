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
using System.Text;
using System.Threading.Tasks;

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


			

			RankingItems = new ObservableCollection<VideoInfoControlViewModel>();



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


		private async void UpdateRankingList(RankingCategory category)
		{
			var target = SelectedRankingTarget.Value.TargetType;
			var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;


			_PageManager.PageTitle = $"「{category.ToCultulizedText()}」のランキング";

			RankingItems.Clear();

			try
			{
				var listItems = await NiconicoRanking.GetRankingData(target, timeSpan, category);

				for (uint i = 0; i < listItems.Channel.Items.Count; i++)
				{
					var item = listItems.Channel.Items[(int)i];
					var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(item.GetVideoId());
					try
					{
						var videoInfoVM = new RankedVideoInfoControlViewModel(
							i + 1
							, nicoVideo
							, _PageManager
						);

						RankingItems.Add(videoInfoVM);
					}
					catch { }
				}
			}
			catch
			{
				// Errorを通知
			}


			// サムネイル情報を非同期読み込み
			foreach (var videoInfoVM in RankingItems)
			{
				videoInfoVM.LoadThumbnail();
			}
		}


		private async void LoadRankingFromSearchWithPopularity(string parameter)
		{

			var listItems = new List<Mntone.Nico2.Videos.Search.ListItem>();

			// 
			for (uint i = 0; i < 3; i++)
			{
				var res = await ContentFinder.GetKeywordSearch(parameter, i + 1, SortMethod.Popurarity);
				if (res.IsStatusOK)
				{
					listItems.AddRange(res.list);
				}
			}



			for (uint i = 0; i < listItems.Count; i++)
			{
				var item = listItems[(int)i];

				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(item.id);
				try
				{
					var videoInfoVM = new RankedVideoInfoControlViewModel(
						i + 1
						, nicoVideo
						, _PageManager
						);

					RankingItems.Add(videoInfoVM);
				}
				catch { }
			}

			// サムネイル情報を非同期読み込み
			foreach (var videoInfoVM in RankingItems)
			{
				videoInfoVM.LoadThumbnail();
			}
		}



		internal void ShowVideoInfomation(string videoUrl)
		{
			_PageManager.OpenPage(HohoemaPageType.VideoInfomation, videoUrl);
//			_EventAggregator.GetEvent<Events.PlayNicoVideoEvent>()
//				.Publish(videoUrl);
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);


			if (e.Parameter is string)
			{
				CategoryInfo = RankingCategoryInfo.FromParameterString(e.Parameter as string);
			}
			else
			{
				return;
			}

			RefreshRankingList();

			// TODO: ページタイトルを変更したい

		}


		public void RefreshRankingList()
		{
			IsFailedRefreshRanking.Value = false;

			try
			{
				switch (CategoryInfo.RankingSource)
				{
					case RankingSource.CategoryRanking:
						RankingCategory = (RankingCategory)Enum.Parse(typeof(RankingCategory), CategoryInfo.Parameter);
						UpdateRankingList(RankingCategory);
						CanChangeRankingParameter.Value = true;
						break;
					case RankingSource.SearchWithMostPopular:
						LoadRankingFromSearchWithPopularity(CategoryInfo.Parameter);
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

		public ObservableCollection<VideoInfoControlViewModel> RankingItems { get; private set; }

		private PageManager _PageManager;
		private HohoemaApp HohoemaApp;
		private NiconicoContentFinder ContentFinder;
		private EventAggregator _EventAggregator;
		public RankingSettings RankingSettings { get; private set; }

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

using Mntone.Nico2.Videos.Ranking;
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
	public class RankingPageViewModel : ViewModelBase
	{
		public RankingPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea)
		{
			HohoemaApp = hohoemaApp;
			_EventAggregator = ea;
			RankingSettings = hohoemaApp.UserSettings.RankingSettings;

			// ランキングの対象
			RankingTargetItems = new List<RankingTargetListItem>()
			{
				new RankingTargetListItem(RankingTarget.view),
				new RankingTargetListItem(RankingTarget.res),
				new RankingTargetListItem(RankingTarget.mylist)
			};

			SelectedRankingTarget = new ReactiveProperty<RankingTargetListItem>(RankingTargetItems[0]);


			// ランキングの集計期間
			RankingTimeSpanItems = new List<RankingTimeSpanListItem>()
			{
				new RankingTimeSpanListItem(RankingTimeSpan.hourly),
				new RankingTimeSpanListItem(RankingTimeSpan.daily),
				new RankingTimeSpanListItem(RankingTimeSpan.weekly),
				new RankingTimeSpanListItem(RankingTimeSpan.monthly),
				new RankingTimeSpanListItem(RankingTimeSpan.total),
			};

			SelectedRankingTimeSpan = new ReactiveProperty<RankingTimeSpanListItem>(RankingTimeSpanItems[0]);


			// ランキングのカテゴリ
			// TODO: R-18などは除外しないとUWPとしては出せない
			RankingCategoryItems = new List<RankingCategoryListItem>();
			
			foreach (var categoryType in (IEnumerable<RankingCategory>)Enum.GetValues(typeof(RankingCategory)))
			{
				RankingCategoryItems.Add(new RankingCategoryListItem(categoryType));
			}

			SelectedRankingCategory = new ReactiveProperty<RankingCategoryListItem>(RankingCategoryItems[0]);


			RankingItems = new ObservableCollection<VideoInfoControlViewModel>();



			Observable.CombineLatest(
				SelectedRankingTarget.ToUnit(),
				SelectedRankingCategory.ToUnit(),
				SelectedRankingTimeSpan.ToUnit()
				)
				.Throttle(TimeSpan.FromSeconds(0.25), UIDispatcherScheduler.Default)
				.SubscribeOnUIDispatcher()
				.Subscribe(x => 
				{
					UpdateRankingList();
				});
			
		}


		private async void UpdateRankingList()
		{
			var target = SelectedRankingTarget.Value.TargetType;
			var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
			var category = SelectedRankingCategory.Value.Category;


			RankingItems.Clear();

			try
			{
				var list = await NiconicoRanking.GetRankingData(target, timeSpan, category);

				foreach(var item in list.Channel.Items)
				{
					try
					{
						var videoInfoVM = new VideoInfoControlViewModel(item.Title, item.VideoUrl, HohoemaApp);

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



		internal void PlayVideo(string videoUrl)
		{
			_EventAggregator.GetEvent<Events.PlayNicoVideoEvent>()
				.Publish(videoUrl);
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
			Debug.WriteLine("MainPageにきた");
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnNavigatingFrom(e, viewModelState, suspending);
			Debug.WriteLine("MainPageから去る");
		}

		public List<RankingTargetListItem> RankingTargetItems { get; private set; }
		public ReactiveProperty<RankingTargetListItem> SelectedRankingTarget { get; private set; }

		public List<RankingTimeSpanListItem> RankingTimeSpanItems { get; private set; }
		public ReactiveProperty<RankingTimeSpanListItem> SelectedRankingTimeSpan { get; private set; }

		public List<RankingCategoryListItem> RankingCategoryItems { get; private set; }
		public ReactiveProperty<RankingCategoryListItem> SelectedRankingCategory { get; private set; }


		public ObservableCollection<VideoInfoControlViewModel> RankingItems { get; private set; }

		private HohoemaApp HohoemaApp;
		private EventAggregator _EventAggregator;
		public RankingSettings RankingSettings { get; private set; }

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


	public class RankingCategoryListItem : BindableBase
	{
		public RankingCategoryListItem(RankingCategory category)
		{
			Category = category;
			Label = category.ToCultulizedText();
		}

		public string Label { get; private set; }

		public RankingCategory Category { get; private set; }
	}

}

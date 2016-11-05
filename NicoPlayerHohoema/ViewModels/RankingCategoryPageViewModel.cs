using Mntone.Nico2;
using Mntone.Nico2.Searches.Video;
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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class RankingCategoryPageViewModel : HohoemaVideoListingPageViewModelBase<RankedVideoInfoControlViewModel>
	{
		public RankingCategoryPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService)
		{
			ContentFinder = HohoemaApp.ContentFinder;
			_EventAggregator = ea;

			//			RankingSettings = hohoemaApp.UserSettings.RankingSettings;
			IsFailedRefreshRanking = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			CanChangeRankingParameter = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);



			// ランキングの対象
			RankingTargetItems = new List<RankingTargetListItem>()
			{
				new RankingTargetListItem(RankingTarget.view),
				new RankingTargetListItem(RankingTarget.res),
				new RankingTargetListItem(RankingTarget.mylist)
			};

			SelectedRankingTarget = new ReactiveProperty<RankingTargetListItem>(RankingTargetItems[0], ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);


			// ランキングの集計期間
			RankingTimeSpanItems = new List<RankingTimeSpanListItem>()
			{
				new RankingTimeSpanListItem(RankingTimeSpan.hourly),
				new RankingTimeSpanListItem(RankingTimeSpan.daily),
				new RankingTimeSpanListItem(RankingTimeSpan.weekly),
				new RankingTimeSpanListItem(RankingTimeSpan.monthly),
				new RankingTimeSpanListItem(RankingTimeSpan.total),
			};

			SelectedRankingTimeSpan = new ReactiveProperty<RankingTimeSpanListItem>(RankingTimeSpanItems[0], ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);

			Observable.Merge(
				SelectedRankingTimeSpan.ToUnit(),
				SelectedRankingTarget.ToUnit()
				)
				.SubscribeOnUIDispatcher()
				.Subscribe(async x => 
				{
					// NavigateToが呼ばれた後
					if (RequireCategoryInfo != null || CategoryInfo != null)
					{
						await ResetList();
					}
				})
				.AddTo(_CompositeDisposable);

		}



		internal void ShowVideoInfomation(string videoUrl)
		{
			PageManager.OpenPage(HohoemaPageType.VideoInfomation, videoUrl);
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				RequireCategoryInfo = RankingCategoryInfo.FromParameterString(e.Parameter as string);
			}
			else
			{
				RequireCategoryInfo = null;
			}

			if (RequireCategoryInfo.RankingSource == RankingSource.CategoryRanking)
			{
				RankingCategory category;
				if (Enum.TryParse(RequireCategoryInfo.Parameter, out category))
				{
					var text = RankingCategoryExtention.ToCultulizedText(category);
					UpdateTitle($"{text} のランキング ");
				}
				else
				{
					UpdateTitle($"{RequireCategoryInfo.Parameter} のランキング");
				}
			}
			else
			{
				UpdateTitle($"{RequireCategoryInfo.Parameter} のランキング");
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		
		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			RankingSettings.Save().ConfigureAwait(false);

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}

		#region Implement HohoemaVideListViewModelBase

		protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			IsFailedRefreshRanking.Value = false;

			var categoryInfo = RequireCategoryInfo != null ? RequireCategoryInfo : CategoryInfo;

			IIncrementalSource <RankedVideoInfoControlViewModel> source = null;
			try
			{
				switch (categoryInfo.RankingSource)
				{
					case RankingSource.CategoryRanking:
						RankingCategory = (RankingCategory)Enum.Parse(typeof(RankingCategory), categoryInfo.Parameter);
						var target = SelectedRankingTarget.Value.TargetType;
						var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
						source = new CategoryRankingLoadingSource(HohoemaApp, PageManager, RankingCategory, target, timeSpan);

						CanChangeRankingParameter.Value = true;
						break;
					case RankingSource.SearchWithMostPopular:

						source = new CustomRankingLoadingSource(HohoemaApp, PageManager, categoryInfo.Parameter);

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


			return source;
		}


		protected override void PostResetList()
		{
			if (RequireCategoryInfo != null)
			{
				CategoryInfo = RequireCategoryInfo;
				RequireCategoryInfo = null;

				
			}
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (RequireCategoryInfo != null)
			{
				return !RequireCategoryInfo.Equals(CategoryInfo)
					|| !(mode == NavigationMode.Back || mode == NavigationMode.Forward);
			}
			else
			{
				return !(mode == NavigationMode.Back || mode == NavigationMode.Forward);
			}
		}

		#endregion

		


		public ReactiveProperty<bool> IsFailedRefreshRanking { get; private set; }

		public ReactiveProperty<bool> CanChangeRankingParameter { get; private set; }


		public RankingCategoryInfo RequireCategoryInfo { get; private set; }
		public RankingCategoryInfo CategoryInfo { get; private set; }

		public RankingCategory RankingCategory { get; private set; }

		public List<RankingTargetListItem> RankingTargetItems { get; private set; }
		public ReactiveProperty<RankingTargetListItem> SelectedRankingTarget { get; private set; }

		public List<RankingTimeSpanListItem> RankingTimeSpanItems { get; private set; }
		public ReactiveProperty<RankingTimeSpanListItem> SelectedRankingTimeSpan { get; private set; }

		private NiconicoContentFinder ContentFinder;
		private EventAggregator _EventAggregator;
		public RankingSettings RankingSettings
		{
			get
			{
				return HohoemaApp.UserSettings.RankingSettings;
			}
		}
	}


	public class CategoryRankingLoadingSource : HohoemaVideoPreloadingIncrementalSourceBase<RankedVideoInfoControlViewModel>
	{
		NiconicoVideoRss RankingRss;
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
		RankingCategory _Category;
		RankingTarget _Target;
		RankingTimeSpan _TimeSpan;

		

		public CategoryRankingLoadingSource(HohoemaApp app, PageManager pageManager, RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
			: base(app, "CategoryRanking_" + category.ToString())
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
			_Category = category;
			_Target = target;
			_TimeSpan = timeSpan;
		}




		#region Implements HohoemaPreloadingIncrementalSourceBase		


		protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
		{
			if (RankingRss != null)
			{
				var items = RankingRss.Channel.Items.Skip(start).Take(count);

				List<NicoVideo> videos = new List<NicoVideo>();
				foreach (var item in items)
				{
					var videoId = item.GetVideoId();
					var nicoVideo = await ToNicoVideo(videoId);

					nicoVideo.PreSetTitle(item.Title);
					nicoVideo.PreSetPostAt(DateTime.Parse(item.PubDate));

					videos.Add(nicoVideo);
				}

				return videos;
			}
			else
			{
				return Enumerable.Empty<NicoVideo>();
			}
		}


		protected override async Task<int> HohoemaPreloadingResetSourceImpl()
		{
			RankingRss = await NiconicoRanking.GetRankingData(_Target, _TimeSpan, _Category);
			return RankingRss.Channel.Items.Count;
		}



		protected override RankedVideoInfoControlViewModel NicoVideoToTemplatedItem(
			NicoVideo itemSource
			, int index
			)
		{
			return new RankedVideoInfoControlViewModel(
					(uint)(index + 1)
					, itemSource
					, _PageManager
				);
		}


		#endregion


		

	}



	public class CustomRankingLoadingSource : HohoemaVideoPreloadingIncrementalSourceBase<RankedVideoInfoControlViewModel>
	{
		PageManager _PageManager;
		string _Parameter;

	
		public CustomRankingLoadingSource(HohoemaApp app, PageManager pageManager, string parameter)
			 : base(app, "CustomRanking_" + parameter)
		{
			_PageManager = pageManager;
			_Parameter = parameter;
		}


		#region Implements HohoemaPreloadingIncrementalSourceBase		


		protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
		{
			var contentFinder = HohoemaApp.ContentFinder;
			var mediaManager = HohoemaApp.MediaManager;

			var response = await HohoemaApp.ContentFinder.GetKeywordSearch(_Parameter, (uint)start, (uint)count, Sort.Popurarity);


			List<NicoVideo> videos = new List<NicoVideo>();
			foreach (var item in response.VideoInfoItems)
			{
				var videoId = item.Video.Id;
				var nicoVideo = await ToNicoVideo(videoId);

				nicoVideo.PreSetTitle(item.Video.Title);
				nicoVideo.PreSetPostAt(item.Video.FirstRetrieve);

				videos.Add(nicoVideo);
			}

			return videos;
		}


		protected override async Task<int> HohoemaPreloadingResetSourceImpl()
		{
			var contentFinder = HohoemaApp.ContentFinder;

			var res = await contentFinder.GetKeywordSearch(_Parameter, 0, 1, Sort.Popurarity).ConfigureAwait(false);

			return (int)res.GetTotalCount();
		}



		protected override RankedVideoInfoControlViewModel NicoVideoToTemplatedItem(
			NicoVideo itemSource
			, int index
			)
		{
			return new RankedVideoInfoControlViewModel(
					(uint)(index + 1)
					, itemSource
					, _PageManager
				);
		}


		#endregion
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

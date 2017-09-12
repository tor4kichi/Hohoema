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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class RankingCategoryPageViewModel : HohoemaVideoListingPageViewModelBase<RankedVideoInfoControlViewModel>
	{
		public RankingCategoryPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea, PageManager pageManager)
			: base(hohoemaApp, pageManager, useDefaultPageTitle:false)
		{
            this.ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);

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
                var text = RankingCategoryExtention.ToCultulizedText(RequireCategoryInfo.Category);
                UpdateTitle($"{text} のランキング ");
                RankingCategory = RequireCategoryInfo.Category;
            }
			else
			{
				RequireCategoryInfo = null;
                UpdateTitle($"? のランキング ");
            }

			
			base.OnNavigatedTo(e, viewModelState);
		}

		
		protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);
		}

		#region Implement HohoemaVideListViewModelBase

		protected override IIncrementalSource<RankedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			IsFailedRefreshRanking.Value = false;

			var categoryInfo = RequireCategoryInfo != null ? RequireCategoryInfo : CategoryInfo;

			IIncrementalSource <RankedVideoInfoControlViewModel> source = null;
			try
			{
                var target = SelectedRankingTarget.Value.TargetType;
                var timeSpan = SelectedRankingTimeSpan.Value.TimeSpan;
                source = new CategoryRankingLoadingSource(HohoemaApp, PageManager, RankingCategory, target, timeSpan);

                CanChangeRankingParameter.Value = true;
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
					|| mode == NavigationMode.New;
			}
			else
			{
                return base.CheckNeedUpdateOnNavigateTo(mode);
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
			: base(app, $"Ranking:{category.ToCultulizedText()}")
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
			_Category = category;
			_Target = target;
			_TimeSpan = timeSpan;
		}


        readonly Regex RankingRankPrefixPatternRegex = new Regex("(^第\\d*位：)");

//        List<NicoVideo> Videos = new List<NicoVideo>();

        #region Implements HohoemaPreloadingIncrementalSourceBase		

        public override uint OneTimeLoadCount => 10;

        protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
		{
            await Task.Delay(0);

            if (RankingRss != null)
            {
                var items = RankingRss.Channel.Items.Skip(start).Take(count).ToArray();

                var nicoVideos = await HohoemaApp.MediaManager.GetNicoVideoItemsAsync(items.Select(x => x.GetVideoId()).ToArray());

                for (var index = 0; index < nicoVideos.Count; ++index)
                {
                    var item = items[index];
                    var nicoVideo = nicoVideos[index];


                    var title = RankingRankPrefixPatternRegex.Replace(item.Title, "");

                    nicoVideo.PreSetTitle(title);
                    //					nicoVideo.PreSetPostAt(DateTime.Parse(item.PubDate));
                }

                return nicoVideos;
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

using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Helpers;
using Mntone.Nico2.Searches.Live;
using Prism.Commands;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Mntone.Nico2;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using System.Collections.Async;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultLivePageViewModel : HohoemaListingPageViewModelBase<LiveInfoViewModel>
	{
        public class LiveSearchSortOptionListItem
        {
            public string Label { get; set; }
            public NicoliveSearchSort Sort { get; set; }
            public Order Order { get; set; }
        }

        public class LiveSearchModeOptionListItem
        {
            public string Label { get; set; }
            public NicoliveSearchMode? Mode { get; set; }
        }

        public class LiveSearchProviderOptionListItem
        {
            public string Label { get; set; }
            public Mntone.Nico2.Live.CommunityType? Provider { get; set; }
        }

        public static IReadOnlyList<LiveSearchSortOptionListItem> LiveSearchSortOptionListItems { get; private set; }
        public static IReadOnlyList<LiveSearchModeOptionListItem> LiveSearchModeOptionListItems { get; private set; }
        public static IReadOnlyList<LiveSearchProviderOptionListItem> LiveSearchProviderOptionListItems { get; private set; }

        static SearchResultLivePageViewModel()
        {
            var sortList = new[]
            {
                NicoliveSearchSort.Recent,
                NicoliveSearchSort.Comment,
            };

            LiveSearchSortOptionListItems = sortList.SelectMany(x =>
            {
                return new List<LiveSearchSortOptionListItem>()
                {
                    new LiveSearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Ascending,
                    },
                    new LiveSearchSortOptionListItem()
                    {
                        Sort = x,
                        Order = Order.Descending,
                    },
                };
            })
            .ToList();

            foreach (var item in LiveSearchSortOptionListItems)
            {
                item.Label = Helpers.SortHelper.ToCulturizedText(item.Sort, item.Order);
            }


            LiveSearchModeOptionListItems = new List<LiveSearchModeOptionListItem>()
            {
                new LiveSearchModeOptionListItem()
                {
                    Label = "放送中",
                    Mode = NicoliveSearchMode.OnAir
                },
                new LiveSearchModeOptionListItem()
                {
                    Label = "放送予定",
                    Mode = NicoliveSearchMode.Reserved
                },
                /*
				new LiveSearchModeOptionListItem()
				{
					Label = "放送終了",
					Mode = NicoliveSearchMode.Closed
				},
				new LiveSearchModeOptionListItem()
				{
					Label = "すべて",
					Mode = null
				},
                */
			};


            LiveSearchProviderOptionListItems = new List<LiveSearchProviderOptionListItem>()
            {
                new LiveSearchProviderOptionListItem()
                {
                    Label = "すべて",
                    Provider = null,
                },

                new LiveSearchProviderOptionListItem()
                {
                    Label = "公式",
                    Provider = Mntone.Nico2.Live.CommunityType.Official,
                },
                new LiveSearchProviderOptionListItem()
                {
                    Label = "チャンネル",
                    Provider = Mntone.Nico2.Live.CommunityType.Channel,
                },
                new LiveSearchProviderOptionListItem()
                {
                    Label = "ユーザー",
                    Provider = Mntone.Nico2.Live.CommunityType.Community,
                },

            };
        }

        public ReactiveProperty<LiveSearchSortOptionListItem> SelectedSearchSort { get; private set; }
        public ReactiveProperty<LiveSearchModeOptionListItem> SelectedSearchMode { get; private set; }
        public ReactiveProperty<bool> IsTagSearch { get; private set; }
        public ReactiveProperty<LiveSearchProviderOptionListItem> SelectedProvider { get; private set; }


        public LiveSearchPagePayloadContent SearchOption { get; private set; }



        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }


        public static List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

        public ReactiveProperty<SearchTarget> SelectedSearchTarget { get; }

        private DelegateCommand<SearchTarget?> _ChangeSearchTargetCommand;
        public DelegateCommand<SearchTarget?> ChangeSearchTargetCommand
        {
            get
            {
                return _ChangeSearchTargetCommand
                    ?? (_ChangeSearchTargetCommand = new DelegateCommand<SearchTarget?>(target =>
                    {
                        if (target.HasValue && target.Value != SearchOption.SearchTarget)
                        {
                            var payload = SearchPagePayloadContentHelper.CreateDefault(target.Value, SearchOption.Keyword);
                            PageManager.Search(payload);
                        }
                    }));
            }
        }

        public SearchResultLivePageViewModel(
			HohoemaApp app,
			PageManager pageManager
			) 
			: base(app, pageManager, useDefaultPageTitle: false)
		{
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);

            SelectedSearchSort = new ReactiveProperty<LiveSearchSortOptionListItem>(LiveSearchSortOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedSearchMode = new ReactiveProperty<LiveSearchModeOptionListItem>(LiveSearchModeOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectedProvider = new ReactiveProperty<LiveSearchProviderOptionListItem>(LiveSearchProviderOptionListItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();

            Observable.Merge(
                SelectedSearchSort.ToUnit(),
                SelectedSearchMode.ToUnit(),
                SelectedProvider.ToUnit()
                )
                .Subscribe(async _ =>
                {
                    if (_NowNavigatingTo) { return; }

                    var selected = SelectedSearchSort.Value;
                    if (SearchOption.Order == selected.Order
                        && SearchOption.Sort == selected.Sort
                        && SearchOption.Mode == SelectedSearchMode.Value?.Mode
                        && SearchOption.Provider == SelectedProvider.Value?.Provider
                    )
                    {
                        return;
                    }

                    SearchOption.Mode = SelectedSearchMode.Value.Mode;
                    SearchOption.Provider = SelectedProvider.Value.Provider;
                    SearchOption.Sort = SelectedSearchSort.Value.Sort;
                    SearchOption.Order = SelectedSearchSort.Value.Order;

                    await ResetList();
                })
                .AddTo(_CompositeDisposable);

        }

        #region Commands


        private DelegateCommand _ShowSearchHistoryCommand;
		public DelegateCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.Search);
					}));
			}
		}

        #endregion

        protected override string ResolvePageName()
        {
            return $"\"{SearchOption.Keyword}\"";
        }

        bool _NowNavigatingTo = false;

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string && e.NavigationMode == NavigationMode.New)
            {
                SearchOption = PagePayloadBase.FromParameterString<LiveSearchPagePayloadContent>(e.Parameter as string);
            }

            SelectedSearchTarget.Value = SearchOption?.SearchTarget ?? SearchTarget.Niconama;

            if (SearchOption == null)
            {
                throw new Exception();
            }

            _NowNavigatingTo = true;
            SelectedSearchSort.Value = LiveSearchSortOptionListItems.FirstOrDefault(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);
            SelectedSearchMode.Value = LiveSearchModeOptionListItems.FirstOrDefault(x => x.Mode == SearchOption.Mode) ?? LiveSearchModeOptionListItems.First();
            SelectedProvider.Value = LiveSearchProviderOptionListItems.FirstOrDefault(x => x.Provider == SearchOption.Provider);
            _NowNavigatingTo = false;


            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);


            base.OnNavigatedTo(e, viewModelState);
        }

        private void ResetSearchOptionText()
        {
            var optionText = Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
            var providerText = SelectedProvider.Value.Label;
            string mode = "";
            if (SearchOption.Mode.HasValue)
            {
                switch (SearchOption.Mode)
                {
                    case Mntone.Nico2.Searches.Live.NicoliveSearchMode.OnAir:
                        mode = "放送中";
                        break;
                    case Mntone.Nico2.Searches.Live.NicoliveSearchMode.Reserved:
                        mode = "放送予定";
                        break;
                    case Mntone.Nico2.Searches.Live.NicoliveSearchMode.Closed:
                        mode = "放送終了";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                mode = "すべて";
            }

            SearchOptionText = $"{optionText}/{mode}/{providerText}";
        }

        protected override void PostResetList()
        {
            ResetSearchOptionText();

            base.PostResetList();
        }


        protected override IIncrementalSource<LiveInfoViewModel> GenerateIncrementalSource()
		{
			return new LiveSearchSource(SearchOption, HohoemaApp, PageManager);
		}
	}



	public class LiveSearchSource : IIncrementalSource<LiveInfoViewModel>
	{

		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		public LiveSearchPagePayloadContent SearchOption { get; private set; }

		public List<Tag> Tags { get; private set; }

		public uint OneTimeLoadCount => 10;

        public List<VideoInfo> Info { get; } = new List<VideoInfo>();

		public LiveSearchSource(
			LiveSearchPagePayloadContent searchOption,
			HohoemaApp app,
			PageManager pageManager
			)
		{
			HohoemaApp = app;
			PageManager = pageManager;
			SearchOption = searchOption;
		}


        public HashSet<string> SearchedVideoIdsHash = new HashSet<string>();

		private Task<NicoliveVideoResponse> GetLiveSearchResponseOnCurrentOption(uint from, uint length)
		{
			return HohoemaApp.ContentProvider.LiveSearchAsync(
					SearchOption.Keyword,
					SearchOption.IsTagSearch,
					from: from,
					length: length,
					provider: SearchOption.Provider,
					sort: SearchOption.Sort,
					order: SearchOption.Order,
					mode: SearchOption.Mode
					);
		}

		public async Task<int> ResetSource()
		{
			int totalCount = 0;
			try
			{
				var res = await GetLiveSearchResponseOnCurrentOption(0, 30);

                if (res.VideoInfo != null)
                {
                    foreach (var v in res.VideoInfo)
                    {
                        AddLive(v);
                    }
                }

                Tags = res.Tags?.Tag.ToList();
                totalCount = res.TotalCount.FilteredCount;
			}
			catch { }

			return totalCount;
		}

        private void AddLive(VideoInfo live)
        {
            if (!SearchedVideoIdsHash.Contains(live.Video.Id))
            {
                Info.Add(live);
                SearchedVideoIdsHash.Add(live.Video.Id);
            }
        }
		public async Task<IAsyncEnumerable<LiveInfoViewModel>> GetPagedItems(int head, int count)
		{
            if (Info.Count < (head + count))
            {
                var res = await GetLiveSearchResponseOnCurrentOption((uint)Info.Count, (uint)count);
                Tags = res.Tags?.Tag.ToList();

                if (res.VideoInfo != null)
                {
                    foreach (var v in res.VideoInfo)
                    {
                        AddLive(v);
                    }
                }
            }

            return Info.Skip(head).Take(count).Select(x =>
			{
				return new LiveInfoViewModel(x);
			})
            .ToAsyncEnumerable();
		}
	}


	public class LiveInfoViewModel : HohoemaListingPageItemBase, Interfaces.ILiveContent
    {
		public string LiveId { get; private set; }

		public string CommunityName { get; private set; }
		public string CommunityThumbnail { get; private set; }
		public string CommunityGlobalId { get; private set; }
		public Mntone.Nico2.Live.CommunityType CommunityType { get; private set; }

		public string LiveTitle { get; private set; }
		public int ViewCounter { get; private set; }
		public int CommentCount { get; private set; }
		public DateTime OpenTime { get; private set; }
		public DateTime StartTime { get; private set; }
		public bool HasEndTime { get; private set; }
		public DateTime EndTime { get; private set; }
		public string DurationText { get; private set; }
		public bool IsTimeshiftEnabled { get; private set; }
		public bool IsCommunityMemberOnly { get; private set; }

        public bool IsXbox => Helpers.DeviceTypeHelper.IsXbox;


        public string BroadcasterId => CommunityGlobalId;
        public string Id => LiveId;


        public LiveInfoViewModel(Mntone.Nico2.Live.Recommend.LiveRecommendData liveVideoInfo)
        {
            LiveId = "lv" + liveVideoInfo.ProgramId;

            CommunityThumbnail = liveVideoInfo.ThumbnailUrl;

            CommunityGlobalId = liveVideoInfo.DefaultCommunity;
            CommunityType = liveVideoInfo.ProviderType;

            LiveTitle = liveVideoInfo.Title;
            OpenTime = liveVideoInfo.OpenTime.DateTime;
            StartTime = liveVideoInfo.StartTime.DateTime;
            //EndTime = liveVideoInfo.Video.EndTime;
            //IsTimeshiftEnabled = liveVideoInfo.Video.TimeshiftEnabled;
            //IsCommunityMemberOnly = liveVideoInfo.Video.CommunityOnly;

            AddImageUrl(CommunityThumbnail);

            //Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            switch (liveVideoInfo.CurrentStatus)
            {
                case Mntone.Nico2.Live.StatusType.Invalid:
                    break;
                case Mntone.Nico2.Live.StatusType.OnAir:
                    DurationText = $"{StartTime - DateTime.Now} 経過";
                    break;
                case Mntone.Nico2.Live.StatusType.ComingSoon:
                    DurationText = $"開始予定: {StartTime}";
                    break;
                case Mntone.Nico2.Live.StatusType.Closed:
                    DurationText = $"放送終了";
                    break;
                default:
                    break;
            }
            
            OptionText = DurationText;
        }

        public LiveInfoViewModel(VideoInfo liveVideoInfo)
		{
            LiveId = liveVideoInfo.Video.Id;
            CommunityName = liveVideoInfo.Community?.Name;
            if (liveVideoInfo.Community?.Thumbnail != null)
            {
                CommunityThumbnail = liveVideoInfo.Community?.Thumbnail;
            }
            else
            {
                CommunityThumbnail = liveVideoInfo.Video.ThumbnailUrl;
            }
            CommunityGlobalId = liveVideoInfo.Community?.GlobalId;
			CommunityType = liveVideoInfo.Video.ProviderType;

			LiveTitle = liveVideoInfo.Video.Title;
			ViewCounter = int.Parse(liveVideoInfo.Video.ViewCounter);
			CommentCount = int.Parse(liveVideoInfo.Video.CommentCount);
			OpenTime = liveVideoInfo.Video.OpenTime;
			StartTime = liveVideoInfo.Video.StartTime;
			EndTime = liveVideoInfo.Video.EndTime;
			IsTimeshiftEnabled = liveVideoInfo.Video.TimeshiftEnabled;
			IsCommunityMemberOnly = liveVideoInfo.Video.CommunityOnly;

            Label = liveVideoInfo.Video.Title;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

			if (liveVideoInfo.Video.StartTime > DateTime.Now)
			{
				// 予約
				DurationText = $" 開始予定: {liveVideoInfo.Video.StartTime}";
			}
			else if (liveVideoInfo.Video.EndTime > DateTime.Now)
			{
                var duration = DateTime.Now - StartTime;
                // 放送中
                if (duration.Hours > 0)
                {
                    DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
                }
                else
                {
                    DurationText = $"{duration.Minutes}分 経過";
                }
			}
			else
			{
                var duration = EndTime - StartTime;
                // 終了
                if (duration.Hours > 0)
                {
                    DurationText = $"{liveVideoInfo.Video.EndTime} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{liveVideoInfo.Video.EndTime} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }

        
    }
}

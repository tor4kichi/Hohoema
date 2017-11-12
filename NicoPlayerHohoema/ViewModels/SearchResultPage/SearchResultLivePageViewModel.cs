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


            Observable.Merge(
                SelectedSearchSort.ToUnit(),
                SelectedSearchMode.ToUnit(),
                SelectedProvider.ToUnit()
                )
                .Subscribe(_ =>
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

                    pageManager.Search(SearchOption, forgetLastSearch: true);
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


        bool _NowNavigatingTo = false;

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                SearchOption = PagePayloadBase.FromParameterString<LiveSearchPagePayloadContent>(e.Parameter as string);
            }

            if (SearchOption == null)
            {
                throw new Exception();
            }

            _NowNavigatingTo = true;
            SelectedSearchSort.Value = LiveSearchSortOptionListItems.FirstOrDefault(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);
            SelectedSearchMode.Value = LiveSearchModeOptionListItems.FirstOrDefault(x => x.Mode == SearchOption.Mode);
            SelectedProvider.Value = LiveSearchProviderOptionListItems.FirstOrDefault(x => x.Provider == SearchOption.Provider);
            _NowNavigatingTo = false;

            Models.Db.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            var target = "生放送";
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

            //			UpdateTitle($"{SearchOption.Keyword} - {target}/{optionText}({mode})");
            UpdateTitle($"\"{SearchOption.Keyword}\"");
            SearchOptionText = $"{target} - {optionText}/{mode}/{providerText}";


            base.OnNavigatedTo(e, viewModelState);
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
				return new LiveInfoViewModel(x, HohoemaApp.Playlist, PageManager);
			})
            .ToAsyncEnumerable();
		}
	}


	public class LiveInfoViewModel : HohoemaListingPageItemBase, Interfaces.ILiveContent
    {
        public HohoemaPlaylist Playlist { get; private set; }
		public VideoInfo LiveVideoInfo { get; private set; }
		public PageManager PageManager { get; private set; }


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

        public LiveInfoViewModel(VideoInfo liveVideoInfo, HohoemaPlaylist playlist, PageManager pageManager)
		{
			LiveVideoInfo = liveVideoInfo;
			PageManager = pageManager;
            Playlist = playlist;

            LiveId = liveVideoInfo.Video.Id;
            CommunityName = LiveVideoInfo.Community?.Name;
            if (LiveVideoInfo.Community?.Thumbnail != null)
            {
                CommunityThumbnail = LiveVideoInfo.Community?.Thumbnail;
            }
            else
            {
                CommunityThumbnail = LiveVideoInfo.Video.ThumbnailUrl;
            }
            CommunityGlobalId = LiveVideoInfo.Community?.GlobalId;
			CommunityType = LiveVideoInfo.Video.ProviderType;

			LiveTitle = LiveVideoInfo.Video.Title;
			ViewCounter = int.Parse(LiveVideoInfo.Video.ViewCounter);
			CommentCount = int.Parse(LiveVideoInfo.Video.CommentCount);
			OpenTime = LiveVideoInfo.Video.OpenTime;
			StartTime = LiveVideoInfo.Video.StartTime;
			EndTime = LiveVideoInfo.Video.EndTime;
			IsTimeshiftEnabled = LiveVideoInfo.Video.TimeshiftEnabled;
			IsCommunityMemberOnly = LiveVideoInfo.Video.CommunityOnly;

            Label = LiveVideoInfo.Video.Title;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

			if (LiveVideoInfo.Video.StartTime > DateTime.Now)
			{
				// 予約
				DurationText = $" 開始予定: {LiveVideoInfo.Video.StartTime}";
			}
			else if (LiveVideoInfo.Video.EndTime > DateTime.Now)
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
                    DurationText = $"{LiveVideoInfo.Video.EndTime} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{LiveVideoInfo.Video.EndTime} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }

        
    }
}

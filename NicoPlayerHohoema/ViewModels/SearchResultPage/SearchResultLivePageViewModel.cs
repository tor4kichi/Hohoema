using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Util;
using Mntone.Nico2.Searches.Live;
using Prism.Commands;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	public class SearchResultLivePageViewModel : HohoemaListingPageViewModelBase<LiveInfoViewModel>
	{
		public LiveSearchPagePayloadContent SearchOption { get; private set; }


		public SearchResultLivePageViewModel(
			HohoemaApp app,
			PageManager pageManager
			) 
			: base(app, pageManager, useDefaultPageTitle: false)
		{
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);
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

            Models.Db.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);

            var target = "生放送";
			var optionText = Util.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);

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
			
			UpdateTitle($"{SearchOption.Keyword} - {target}/{optionText}({mode})");

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
			return HohoemaApp.ContentFinder.LiveSearchAsync(
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
		public async Task<IEnumerable<LiveInfoViewModel>> GetPagedItems(int head, int count)
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
			});
		}
	}


	public class LiveInfoViewModel : HohoemaListingPageItemBase
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



		public LiveInfoViewModel(VideoInfo liveVideoInfo, HohoemaPlaylist playlist, PageManager pageManager)
		{
			LiveVideoInfo = liveVideoInfo;
			PageManager = pageManager;
            Playlist = playlist;

            LiveId = liveVideoInfo.Video.Id;
            CommunityName = LiveVideoInfo.Community?.Name;
			CommunityThumbnail = LiveVideoInfo.Community?.Thumbnail;
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

            Title = LiveVideoInfo.Video.Title;
            OptionText = LiveVideoInfo.Community?.Name;
            ImageUrlsSource.Add(LiveVideoInfo.Community?.Thumbnail);

            var duration = EndTime - StartTime;
			if (LiveVideoInfo.Video.StartTime < DateTime.Now)
			{
				// 予約
				DurationText = "";
			}
			else if (LiveVideoInfo.Video.EndTime < DateTime.Now)
			{
				// 終了
				if (duration.Hours > 0)
				{
					DurationText = $"（{duration.Hours}時間 {duration.Minutes}分）";
				}
				else
				{
					DurationText = $"（{duration.Minutes}分）";
				}
			}
			else
			{
				// 放送中
				// 終了
				if (duration.Hours > 0)
				{
					DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
				}
				else
				{
					DurationText = $"{duration.Minutes}分 経過";
				}
			}

            Description = DurationText;

        }


        private DelegateCommand _OpenLiveVideoPageCommand;
		public override ICommand PrimaryCommand
		{
			get
			{
				return _OpenLiveVideoPageCommand
					?? (_OpenLiveVideoPageCommand = new DelegateCommand(() =>
					{
                        Playlist.PlayLiveVideo(LiveId, LiveTitle);
					}));
			}
		}


        private DelegateCommand _OpenCommunityPageCommand;
        public DelegateCommand OpenCommunityPageCommand
        {
            get
            {
                return _OpenCommunityPageCommand
                    ?? (_OpenCommunityPageCommand = new DelegateCommand(() =>
                    {
                        if (CommunityGlobalId != null)
                        {
                            PageManager.OpenPage(HohoemaPageType.Community, CommunityGlobalId);
                        }
                    }));
            }
        }

    }
}

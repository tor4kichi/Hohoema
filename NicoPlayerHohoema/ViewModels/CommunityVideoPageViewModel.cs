using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Threading;
using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using System.Diagnostics;
using Mntone.Nico2.Videos.Ranking;
using Prism.Commands;
using System.Windows.Input;
using NicoPlayerHohoema.Interfaces;
using System.Collections.Async;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models.Provider;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommunityVideoPageViewModel : HohoemaListingPageViewModelBase<CommunityVideoInfoControlViewModel>
	{
        public CommunityVideoPageViewModel(
            CommunityProvider communityProvider,
            Services.PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist
            )
            : base(pageManager)
        {
            CommunityProvider = communityProvider;
            HohoemaPlaylist = hohoemaPlaylist;
        }


        public string CommunityId { get; private set; }


		public bool IsValidCommunity { get; private set; }

		public CommunityDetail CommunityDetail { get; private set; }

		private string _CommunityName;
		public string CommunityName
		{
			get { return _CommunityName; }
			set { SetProperty(ref _CommunityName, value); }
		}


        private bool _CanDownload;
        public bool CanDownload
        {
            get { return _CanDownload; }
            set { SetProperty(ref _CanDownload, value); }
        }


		

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			IsValidCommunity = false;

			if (e.Parameter is string)
			{
				var id = e.Parameter as string;

				if (NiconicoRegex.IsCommunityId(id))
				{
					CommunityId = id;
					IsValidCommunity = true;
				}
			}

            base.OnNavigatedTo(e, viewModelState);
		}



		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (IsValidCommunity)
			{
				try
				{
					var res = await CommunityProvider.GetCommunityDetail(CommunityId);
					CommunityDetail = res.CommunitySammary.CommunityDetail;

					CommunityName = CommunityDetail.Name;
				}
				catch
				{
					Debug.WriteLine("コミュ情報取得に失敗");
					IsValidCommunity = false;
				}
			}
			else
			{
				Debug.WriteLine("CommunityID は無効: " + CommunityId);
			}



			await base.ListPageNavigatedToAsync(cancelToken, e, viewModelState);
		}

		protected override IIncrementalSource<CommunityVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			if (false == IsValidCommunity)
			{
				throw new Exception();
			}

			return new CommunityVideoIncrementalSource(CommunityId, (int)CommunityDetail.VideoCount, CommunityProvider);
		}


		private DelegateCommand _OpenCommunityPageCommand;
		public DelegateCommand OpenCommunityPageCommand
		{
			get
			{
				return _OpenCommunityPageCommand
					?? (_OpenCommunityPageCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPage(HohoemaPageType.Community, CommunityId);
					}));
			}
		}

        public CommunityProvider CommunityProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
    }


	public class CommunityVideoIncrementalSource : HohoemaIncrementalSourceBase<CommunityVideoInfoControlViewModel>
	{
        public CommunityProvider CommunityProvider { get; }

		public string CommunityId { get; private set; }
		public int VideoCount { get; private set; }

		public List<NiconicoVideoRssItem> Items { get; private set; } = new List<NiconicoVideoRssItem>();

		public CommunityVideoIncrementalSource(string communityId, int videoCount, CommunityProvider communityProvider)
			: base()
		{
            CommunityProvider = communityProvider;
			CommunityId = communityId;
			VideoCount = videoCount;
		}




		#region Implements HohoemaPreloadingIncrementalSourceBase		

		public override uint OneTimeLoadCount => 9; // RSSの一回のページアイテム数が18個なので、表示スピード考えてその半分

		protected override Task<int> ResetSourceImpl()
		{
			Items = new List<NiconicoVideoRssItem>();
			return Task.FromResult(VideoCount);
		}

		protected override async Task<IAsyncEnumerable<CommunityVideoInfoControlViewModel>> GetPagedItemsImpl(int start, int count)
		{
			if (count >= VideoCount)
			{
				return null;
			}

			var tail = (start + count);
			while (Items.Count < tail)
			{
				try
				{
					var pageCount = (uint)(start / 20) + 1;

					Debug.WriteLine("communitu video : page " + pageCount);
					var videoRss = await CommunityProvider.GetCommunityVideo(CommunityId, pageCount);
					var items = videoRss.Channel.Items;

					Items.AddRange(items);
				}
				catch (Exception ex)
				{
					TriggerError(ex);
					return null;
				}
			}

            return Items.Skip(start).Take(count).Select(x => new CommunityVideoInfoControlViewModel(x)).ToAsyncEnumerable();
		}


		#endregion
	}


	public class CommunityVideoInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.IVideoContent
    {
		public NiconicoVideoRssItem RssItem { get; private set; }


		public string VideoId => NicoVideoIdHelper.UrlToVideoId(RssItem.VideoUrl);

		public CommunityVideoInfoControlViewModel(NiconicoVideoRssItem rssItem)
			: base()
		{
			RssItem = rssItem;

            Label = RssItem.Title;
		}

        public string ProviderId => string.Empty;

        public string ProviderName => string.Empty;

        public UserType ProviderType => UserType.User;

        public string Id => VideoId;
    }

	
}

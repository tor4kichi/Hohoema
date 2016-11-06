using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
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

namespace NicoPlayerHohoema.ViewModels
{
	public class CommunityVideoPageViewModel : HohoemaListingPageViewModelBase<CommunityVideoInfoControlViewModel>
	{
		public string CommunityId { get; private set; }


		public bool IsValidCommunity { get; private set; }

		public CommunityDetail CommunityDetail { get; private set; }

		private string _CommunityName;
		public string CommunityName
		{
			get { return _CommunityName; }
			set { SetProperty(ref _CommunityName, value); }
		}



		public CommunityVideoPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, isRequireSignIn: true)
		{
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
					var res = await HohoemaApp.ContentFinder.GetCommunityDetail(CommunityId);
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

			return new CommunityVideoIncrementalSource(CommunityId, (int)CommunityDetail.VideoCount, HohoemaApp, PageManager);
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


	}


	public class CommunityVideoIncrementalSource : HohoemaIncrementalSourceBase<CommunityVideoInfoControlViewModel>
	{
		HohoemaApp HohoemaApp;
		PageManager _PageManager;

		public string CommunityId { get; private set; }
		public int VideoCount { get; private set; }

		public List<NiconicoVideoRssItem> Items { get; private set; } = new List<NiconicoVideoRssItem>();

		public CommunityVideoIncrementalSource(string communityId, int videoCount, HohoemaApp hohoemaApp, PageManager pageManager)
			: base()
		{
			HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			CommunityId = communityId;
			VideoCount = videoCount;
		}




		#region Implements HohoemaPreloadingIncrementalSourceBase		

		public override uint OneTimeLoadCount => 20; // RSSの一回のページアイテム数が18個なので、表示スピード考えてその半分

		protected override Task<int> ResetSourceImpl()
		{
			Items = new List<NiconicoVideoRssItem>();
			return Task.FromResult(VideoCount);
		}

		protected override async Task<IEnumerable<CommunityVideoInfoControlViewModel>> GetPagedItemsImpl(int start, int count)
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
					var videoRss = await HohoemaApp.ContentFinder.GetCommunityVideo(CommunityId, pageCount);
					var items = videoRss.Channel.Items;

					Items.AddRange(items);
				}
				catch (Exception ex)
				{
					TriggerError(ex);
					return null;
				}
			}


			var videos = new List<CommunityVideoInfoControlViewModel>();
			foreach (var item in Items.Skip(start).Take(count))
			{
				videos.Add(new CommunityVideoInfoControlViewModel(item, _PageManager));
			}

			return videos.AsEnumerable();
		}


		#endregion
	}


	public class CommunityVideoInfoControlViewModel : HohoemaListingPageItemBase
	{
		public PageManager PageManager { get; private set; }
		public NiconicoVideoRssItem RssItem { get; private set; }


		public string Title => RssItem.Title;
		public string VideoId => RssItem.GetVideoId();

		public CommunityVideoInfoControlViewModel(NiconicoVideoRssItem rssItem, PageManager pageManager)
			: base()
		{
			PageManager = pageManager;
			RssItem = rssItem;
		}

		private DelegateCommand _SelectedCommand;
		public override ICommand SelectedCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new DelegateCommand(() => 
					{
						var payload = new Models.VideoPlayPayload()
						{
							VideoId = this.VideoId
						};

						PageManager.OpenPage(HohoemaPageType.VideoPlayer, payload.ToParameterString());
					}));
			}
		}

		public override void Dispose()
		{
			
		}
	}

	
}

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
	public class CommunityVideoPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
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
			: base(hohoemaApp, pageManager, mylistDialogService, isRequireSignIn: true)
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

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
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


	public class CommunityVideoIncrementalSource : HohoemaVideoPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
	{

		PageManager _PageManager;

		public string CommunityId { get; private set; }
		public int VideoCount { get; private set; }

		public List<NiconicoVideoRssItem> Items { get; private set; } = new List<NiconicoVideoRssItem>();

		public CommunityVideoIncrementalSource(string communityId, int videoCount, HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, "CommunityVideo")
		{
			_PageManager = pageManager;
			CommunityId = communityId;
			VideoCount = videoCount;
		}




		#region Implements HohoemaPreloadingIncrementalSourceBase		

		public override uint OneTimeLoadCount => 10; // RSSの一回のページアイテム数が18個なので、表示スピード考えてその半分

		protected override Task<int> HohoemaPreloadingResetSourceImpl()
		{
			Items = new List<NiconicoVideoRssItem>();
			return Task.FromResult(VideoCount);
		}

		protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
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


			List<NicoVideo> videos = new List<NicoVideo>();
			foreach (var item in Items.Skip(start).Take(count))
			{
				var videoId = item.GetVideoId();
				var nicoVideo = await ToNicoVideo(videoId);

				nicoVideo.PreSetTitle(item.Title);
//				nicoVideo.PreSetPostAt(DateTime.Parse(item.PubDate));

				videos.Add(nicoVideo);
			}

			return videos.AsEnumerable();
		}

		protected override VideoInfoControlViewModel NicoVideoToTemplatedItem(NicoVideo sourceNicoVideo, int index)
		{
			return new CommunityVideoInfoControlViewModel(sourceNicoVideo, _PageManager);
		}


		#endregion
	}


	public class CommunityVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public CommunityVideoInfoControlViewModel(NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			
		}
		
		// TODO: コミュ限動画等の表示
	}
}

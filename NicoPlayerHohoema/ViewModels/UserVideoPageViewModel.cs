using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using Mntone.Nico2.Users.Video;
using Prism.Windows.Navigation;
using System.Diagnostics;
using Mntone.Nico2.Users.User;
using System.Threading;
using Prism.Commands;

namespace NicoPlayerHohoema.ViewModels
{
	public class UserVideoPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{
		public UserVideoPageViewModel(HohoemaApp app, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService) 
			: base(app, pageManager, mylistDialogService, isRequireSignIn:true)
		{
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				UserId = e.Parameter as string;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			User = await HohoemaApp.ContentFinder.GetUserDetail(UserId);			
		}

		protected override void PostResetList()
		{
			base.PostResetList();

			var source = IncrementalLoadingItems.Source as UserVideoIncrementalSource;

			if (User != null)
			{
				UpdateTitle(User.Nickname + "さんの投稿動画一覧");
			}
			else
			{
				UpdateTitle("投稿動画一覧");
			}
			UserName = User.Nickname;
		}



		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new UserVideoIncrementalSource(
				UserId,
				User,
				HohoemaApp,
				PageManager
				);
		}


		private DelegateCommand _OpenVideoOwnerUserPageCommand;
		public DelegateCommand OpenVideoOwnerUserPageCommand
		{
			get
			{
				return _OpenVideoOwnerUserPageCommand
					?? (_OpenVideoOwnerUserPageCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPage(HohoemaPageType.UserInfo, UserId);
					}));
			}
		}


		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}


		public UserDetail User { get; private set; }

		
		public string UserId { get; private set; }
	}


	public class UserVideoIncrementalSource : HohoemaVideoPreloadingIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public uint UserId { get; private set; }
		public NiconicoContentFinder ContentFinder { get; private set; }
		public NiconicoMediaManager MediaManager { get; private set; }
		public PageManager PageManager { get; private set; }


		public UserDetail User { get; private set;}

		public List<UserVideoResponse> _ResList;
		
		public UserVideoIncrementalSource(string userId, UserDetail userDetail, HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, "UserVideo_" + userId)
		{
			UserId = uint.Parse(userId);
			User = userDetail;
			ContentFinder = HohoemaApp.ContentFinder;
			MediaManager = HohoemaApp.MediaManager;
			PageManager = pageManager;
			_ResList = new List<UserVideoResponse>();
		}

		#region Implements HohoemaPreloadingIncrementalSourceBase		

		protected override async Task<IEnumerable<NicoVideo>> PreloadNicoVideo(int start, int count)
		{
			var rawPage = ((start) / 30);
			var page = rawPage + 1;

			var res = _ResList.ElementAtOrDefault(rawPage);
			if (res == null)
			{
				try
				{
					res = await ContentFinder.GetUserVideos(UserId, (uint)page);
				}
				catch
				{
					return Enumerable.Empty<NicoVideo>();
				}
				_ResList.Add(res);
			}

			var head = start - rawPage * 30;

			var items = res.Items.Skip(head).Take(count);
			List<NicoVideo> videos = new List<NicoVideo>();
			foreach (var item in items)
			{
				var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.VideoId);

				nicoVideo.PreSetTitle(item.Title);
				nicoVideo.PreSetThumbnailUrl(item.ThumbnailUrl.AbsoluteUri);
				nicoVideo.PreSetVideoLength(item.Length);

				videos.Add(nicoVideo);
			}

			return videos;
		}

		protected override async Task<int> ResetSourceImpl()
		{
			User = await ContentFinder.GetUserDetail(UserId.ToString());
			return (int)User.TotalVideoCount;
		}


		protected override VideoInfoControlViewModel NicoVideoToTemplatedItem(
			NicoVideo itemSource
			, int index
			)
		{
			return new VideoInfoControlViewModel(itemSource, PageManager);
		}

		#endregion
	}
}

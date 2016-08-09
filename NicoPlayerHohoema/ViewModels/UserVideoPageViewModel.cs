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

		protected override uint IncrementalLoadCount
		{
			get
			{
				return 30;
			}
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

		}



		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new UserVideoIncrementalSource(
				UserId,
				User,
				HohoemaApp.ContentFinder,
				HohoemaApp.MediaManager,
				PageManager
				);
		}

		public UserDetail User { get; private set; }


		public string UserId { get; private set; }
	}


	public class UserVideoIncrementalSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public uint UserId { get; private set; }
		public NiconicoContentFinder ContentFinder { get; private set; }
		public NiconicoMediaManager MediaManager { get; private set; }
		public PageManager PageManager { get; private set; }


		public UserDetail User { get; private set;}

		public async Task<int> ResetSource()
		{
			User = await ContentFinder.GetUserDetail(UserId.ToString());
			return (int)User.TotalVideoCount;
		}

		public UserVideoIncrementalSource(string userId, UserDetail userDetail, NiconicoContentFinder contentFinder, NiconicoMediaManager mediaMan, PageManager pageManager)
		{
			UserId = uint.Parse(userId);
			User = userDetail;
			ContentFinder = contentFinder;
			MediaManager = mediaMan;
			PageManager = pageManager;
		}

		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			if (User.TotalVideoCount < pageIndex)
			{
				return Enumerable.Empty<VideoInfoControlViewModel>();
			}


			var list = new List<VideoInfoControlViewModel>();
			var page = ((pageIndex - 1) / pageSize) + 1;

			try
			{
				var res = await ContentFinder.GetUserVideos(UserId, page);

				foreach (var item in res.Items)
				{
					var nicoVideo = await MediaManager.GetNicoVideo(item.VideoId);
					var vm = new VideoInfoControlViewModel(nicoVideo, PageManager);

					list.Add(vm);

					await vm.LoadThumbnail();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

			return list;
		}
	}
}

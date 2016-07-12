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

namespace NicoPlayerHohoema.ViewModels
{
	public class UserVideoPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{
		public UserVideoPageViewModel(HohoemaApp app, PageManager pageManager) 
			: base(app, pageManager, isRequireSignIn:true)
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


		protected override uint IncrementalLoadCount
		{
			get
			{
				return 30;
			}
		}

		public override string GetPageTitle()
		{
			return "ユーザーの投稿動画一覧";
		}

		

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new UserVideoIncrementalSource(
				UserId,
				HohoemaApp.ContentFinder,
				HohoemaApp.MediaManager,
				PageManager
				);
		}


		public string UserId { get; private set; }
		
	}


	public class UserVideoIncrementalSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public string UserId { get; private set; }
		public NiconicoContentFinder ContentFinder { get; private set; }
		public NiconicoMediaManager MediaManager { get; private set; }
		public PageManager PageManager { get; private set; }




		UserDetail _User;

		public UserVideoIncrementalSource(string userId, NiconicoContentFinder contentFinder, NiconicoMediaManager mediaMan, PageManager pageManager)
		{
			UserId = userId;
			ContentFinder = contentFinder;
			MediaManager = mediaMan;
			PageManager = pageManager;
		}

		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			if (_User == null)
			{
				_User = await ContentFinder.GetUserDetail(UserId);
			}

			if (_User.TotalVideoCount < pageIndex)
			{
				return Enumerable.Empty<VideoInfoControlViewModel>();
			}


			var list = new List<VideoInfoControlViewModel>();
			var page = ((pageIndex - 1) / pageSize) + 1;

			try
			{
				var res = await ContentFinder.GetUserVideos(uint.Parse(UserId), page);

				foreach (var item in res.Items)
				{
					var nicoVideo = await MediaManager.GetNicoVideo(item.VideoId);
					var vm = new VideoInfoControlViewModel(nicoVideo, PageManager);

					list.Add(vm);

					vm.LoadThumbnail();
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

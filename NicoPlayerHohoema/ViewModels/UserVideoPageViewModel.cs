using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using Mntone.Nico2.Users.Video;
using Prism.Windows.Navigation;
using System.Diagnostics;
using Mntone.Nico2.Users.User;
using System.Threading;
using Prism.Commands;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class UserVideoPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{
		public UserVideoPageViewModel(HohoemaApp app, PageManager pageManager) 
			: base(app, pageManager, isRequireSignIn:true)
		{
		}

        protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (User != null)
            {
                UpdateTitle(User.Nickname + "さんの投稿動画一覧");
            }
            else
            {
                UpdateTitle("投稿動画一覧");
            }

            base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            if (e.Parameter is string)
            {
                UserId = e.Parameter as string;
            }

            User = await HohoemaApp.ContentFinder.GetUserDetail(UserId);

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

		protected override void PostResetList()
		{
			base.PostResetList();
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


	public class UserVideoIncrementalSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public uint UserId { get; }
		public NiconicoContentFinder ContentFinder { get; }
		public NiconicoMediaManager MediaManager { get; }
        public HohoemaApp HohoemaApp { get; }
		public PageManager PageManager { get; }


		public UserDetail User { get; private set;}

		public List<UserVideoResponse> _ResList;
		
		public UserVideoIncrementalSource(string userId, UserDetail userDetail, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			UserId = uint.Parse(userId);
			User = userDetail;
			ContentFinder = HohoemaApp.ContentFinder;
			MediaManager = HohoemaApp.MediaManager;
            HohoemaApp = hohoemaApp;
            PageManager = pageManager;
			_ResList = new List<UserVideoResponse>();
		}

        protected override async Task<IAsyncEnumerable<VideoInfoControlViewModel>> GetPagedItemsImpl(int start, int count)
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
                    return AsyncEnumerable.Empty<VideoInfoControlViewModel>();
                }
                _ResList.Add(res);
            }

            var head = start - rawPage * 30;

            var items = res.Items.Skip(head).Take(count);
            return items.Select(x =>
            {
                var nicoVideo = HohoemaApp.MediaManager.GetNicoVideo(x.VideoId);

                nicoVideo.PreSetTitle(x.Title);
                nicoVideo.PreSetThumbnailUrl(x.ThumbnailUrl.AbsoluteUri);
                nicoVideo.PreSetVideoLength(x.Length);

                return new VideoInfoControlViewModel(nicoVideo, PageManager, isNgEnabled: false);
            })
            .ToAsyncEnumerable();
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult((int)User.TotalVideoCount);
        }
    }
}

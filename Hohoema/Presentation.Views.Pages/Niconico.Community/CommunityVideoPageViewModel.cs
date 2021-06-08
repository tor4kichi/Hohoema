using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using System.Diagnostics;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Hohoema.Models.UseCase;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Presentation.ViewModels.Community;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using Microsoft.Toolkit.Collections;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Community
{
	using CommunityFollowContext = FollowContext<ICommunity>;

    public class CommunityInfo : ICommunity
    {
        public string Id { get; internal set; }

        public string Label { get; internal set; }
    }

    public class CommunityVideoPageViewModel : HohoemaListingPageViewModelBase<CommunityVideoInfoViewModel>, IPinablePage, ITitleUpdatablePage
	{
		HohoemaPin IPinablePage.GetPin()
		{
			return new HohoemaPin()
			{
				Label = CommunityName,
				PageType = HohoemaPageType.CommunityVideo,
				Parameter = $"id={CommunityId}"
			};
		}

		IObservable<string> ITitleUpdatablePage.GetTitleObservable()
		{
			return this.ObserveProperty(x => x.CommunityName);
		}


		// Follow
		private CommunityFollowContext _followContext = CommunityFollowContext.Default;
		public CommunityFollowContext FollowContext
		{
			get => _followContext;
			set => SetProperty(ref _followContext, value);
		}


		public CommunityVideoPageViewModel(
			ApplicationLayoutManager applicationLayoutManager,
			CommunityProvider communityProvider,
			CommunityFollowProvider communityFollowProvider,
            PageManager pageManager
			)
        {
			ApplicationLayoutManager = applicationLayoutManager;
			CommunityProvider = communityProvider;
            _communityFollowProvider = communityFollowProvider;
            PageManager = pageManager;
		}


        public string CommunityId { get; private set; }

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
		
        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string id))
            {
                CommunityId = id;

                try
                {
                    var res = await CommunityProvider.GetCommunityDetail(CommunityId);
                    CommunityDetail = res.CommunitySammary.CommunityDetail;

                    CommunityName = CommunityDetail.Name;

				}
				catch
                {
                    Debug.WriteLine("コミュ情報取得に失敗");
                }


				try
                {
					FollowContext = CommunityFollowContext.Default;
					var authority = await _communityFollowProvider.GetCommunityAuthorityAsync(CommunityId);
					if (!authority.Data.IsOwner)
					{
						if (!string.IsNullOrWhiteSpace(CommunityId))
						{
							FollowContext = await CommunityFollowContext.CreateAsync(_communityFollowProvider, new CommunityInfo() { Id = CommunityId, Label = CommunityName });
						}
					}
				}
                catch
                {
					FollowContext = CommunityFollowContext.Default;
				}
			}

			await base.OnNavigatedToAsync(parameters);
        }



		protected override (int, IIncrementalSource<CommunityVideoInfoViewModel>) GenerateIncrementalSource()
		{
			return (CommunityVideoIncrementalSource.OneTimeLoadCount, new CommunityVideoIncrementalSource(CommunityId, (int)CommunityDetail.VideoCount, CommunityProvider));
		}

        private DelegateCommand _OpenCommunityPageCommand;
        private readonly CommunityFollowProvider _communityFollowProvider;

        public DelegateCommand OpenCommunityPageCommand
		{
			get
			{
				return _OpenCommunityPageCommand
					?? (_OpenCommunityPageCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPageWithId(HohoemaPageType.Community, CommunityId);
					}));
			}
		}

		public ApplicationLayoutManager ApplicationLayoutManager { get; }
		public CommunityProvider CommunityProvider { get; }
        public PageManager PageManager { get; }
	}


	public class CommunityVideoIncrementalSource : IIncrementalSource<CommunityVideoInfoViewModel>
	{
        public CommunityProvider CommunityProvider { get; }

		public string CommunityId { get; private set; }
		public int VideoCount { get; private set; }

		public List<RssVideoData> Items { get; private set; } = new List<RssVideoData>();

		public CommunityVideoIncrementalSource(string communityId, int videoCount, CommunityProvider communityProvider)
		{
            CommunityProvider = communityProvider;
			CommunityId = communityId;
			VideoCount = videoCount;
		}

		public const int OneTimeLoadCount = 18; // RSSの一回のページアイテム数が18個なので、表示スピード考えてその半分

        async Task<IEnumerable<CommunityVideoInfoViewModel>> IIncrementalSource<CommunityVideoInfoViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
			try
            {
				var videoRss = await CommunityProvider.GetCommunityVideo(CommunityId, (uint)pageIndex);
				if (!videoRss.IsOK || videoRss.Items == null || !videoRss.Items.Any())
				{
					return Enumerable.Empty<CommunityVideoInfoViewModel>();
				}

				return videoRss.Items.Select(x => new CommunityVideoInfoViewModel(x));
			}
            catch (Exception e)
            {
				ErrorTrackingManager.TrackError(e);
				return Enumerable.Empty<CommunityVideoInfoViewModel>();
			}
		}
    }


	
}

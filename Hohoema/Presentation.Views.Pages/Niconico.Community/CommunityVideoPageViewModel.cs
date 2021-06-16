using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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
                    var res = await CommunityProvider.GetCommunityInfo(CommunityId);
                    
                    CommunityName = res.Community.Name;
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
							FollowContext = await CommunityFollowContext.CreateAsync(_communityFollowProvider, new CommunityViewModel() { CommunityId = CommunityId, Name = CommunityName });
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
			return (CommunityVideoIncrementalSource.OneTimeLoadCount, new CommunityVideoIncrementalSource(CommunityId, 1, CommunityProvider));
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

		public CommunityVideoIncrementalSource(string communityId, int videoCount, CommunityProvider communityProvider)
		{
            CommunityProvider = communityProvider;
			CommunityId = communityId;
			VideoCount = videoCount;
		}

		public const int OneTimeLoadCount = 20; 

        async Task<IEnumerable<CommunityVideoInfoViewModel>> IIncrementalSource<CommunityVideoInfoViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
			try
            {
				var head = pageIndex * pageSize;
				var (listRes, itemsRes) = await CommunityProvider.GetCommunityVideoAsync(CommunityId, head, pageSize, sortKey: null, sortOrder: null);
				if (itemsRes == null || !itemsRes.IsSuccess || itemsRes.Data.Videos == null || itemsRes.Data.Videos.Length == 0)
				{
					return Enumerable.Empty<CommunityVideoInfoViewModel>();
				}

				return itemsRes.Data.Videos.Select(x => new CommunityVideoInfoViewModel(x));
			}
            catch (Exception e)
            {
				ErrorTrackingManager.TrackError(e);
				return Enumerable.Empty<CommunityVideoInfoViewModel>();
			}
		}
    }


	
}

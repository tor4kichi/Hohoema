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

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Video
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



		protected override IIncrementalSource<CommunityVideoInfoViewModel> GenerateIncrementalSource()
		{
			return new CommunityVideoIncrementalSource(CommunityId, (int)CommunityDetail.VideoCount, CommunityProvider);
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


	public class CommunityVideoIncrementalSource : HohoemaIncrementalSourceBase<CommunityVideoInfoViewModel>
	{
        public CommunityProvider CommunityProvider { get; }

		public string CommunityId { get; private set; }
		public int VideoCount { get; private set; }

		public List<RssVideoData> Items { get; private set; } = new List<RssVideoData>();

		public CommunityVideoIncrementalSource(string communityId, int videoCount, CommunityProvider communityProvider)
			: base()
		{
            CommunityProvider = communityProvider;
			CommunityId = communityId;
			VideoCount = videoCount;
		}




		#region Implements HohoemaPreloadingIncrementalSourceBase		

		public override uint OneTimeLoadCount => 9; // RSSの一回のページアイテム数が18個なので、表示スピード考えてその半分

		protected override ValueTask<int> ResetSourceImpl()
		{
			Items = new List<RssVideoData>();
			return new ValueTask<int>(VideoCount);
		}

		protected override async IAsyncEnumerable<CommunityVideoInfoViewModel> GetPagedItemsImpl(int start, int count, [EnumeratorCancellation] CancellationToken ct = default)
		{
			if (count >= VideoCount)
			{
				yield break;
			}

			var tail = (start + count);
			while (Items.Count < tail)
			{
				try
				{
					var pageCount = (uint)(start / 20) + 1;

					Debug.WriteLine("communitu video : page " + pageCount);
					var videoRss = await CommunityProvider.GetCommunityVideo(CommunityId, pageCount);
					var items = videoRss.Items;

					Items.AddRange(items);
				}
				catch (Exception ex)
				{
					TriggerError(ex);
					yield break;
				}

				ct.ThrowIfCancellationRequested();
			}

			foreach (var item in Items.Skip(start).Take(count))
            {
				var vm = new CommunityVideoInfoViewModel(item);
				yield return vm;

				ct.ThrowIfCancellationRequested();
			}
		}


		#endregion
	}


	
}

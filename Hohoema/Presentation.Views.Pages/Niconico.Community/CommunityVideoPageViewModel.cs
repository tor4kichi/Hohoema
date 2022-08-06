using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Presentation.Navigations;
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
using NiconicoToolkit.Community;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Domain.Playlist;
using Reactive.Bindings;
using Microsoft.Extensions.Logging;
using ZLogger;

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
			ILoggerFactory loggerFactory,
			ApplicationLayoutManager applicationLayoutManager,
			CommunityProvider communityProvider,
			CommunityFollowProvider communityFollowProvider,
            PageManager pageManager,
			VideoPlayWithQueueCommand videoPlayWithQueueCommand
			)
			: base(loggerFactory.CreateLogger<CommunityVideoPageViewModel>())
		{
			ApplicationLayoutManager = applicationLayoutManager;
			CommunityProvider = communityProvider;
            _communityFollowProvider = communityFollowProvider;
            PageManager = pageManager;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;

			CurrentPlaylistToken = Observable.CombineLatest(
				this.ObserveProperty(x => x.CommunityVideoPlaylist),
				this.ObserveProperty(x => x.SelectedSortOption),
				(x, y) => new PlaylistToken(x, y)
				)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(_CompositeDisposable);
		}


        public CommunityId? CommunityId { get; private set; }

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


		private CommunityVideoPlaylist _CommunityVideoPlaylist;
		public CommunityVideoPlaylist CommunityVideoPlaylist
		{
			get { return _CommunityVideoPlaylist; }
			private set { SetProperty(ref _CommunityVideoPlaylist, value); }
		}


		public CommunityVideoPlaylistSortOption[] SortOptions => CommunityVideoPlaylist.SortOptions;


		private CommunityVideoPlaylistSortOption _selectedSortOption;
		public CommunityVideoPlaylistSortOption SelectedSortOption
		{
			get { return _selectedSortOption; }
			set { SetProperty(ref _selectedSortOption, value); }
		}


		public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }



		public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
			CommunityId? communityId = null;
			if (parameters.TryGetValue("id", out string strCommunityId))
			{
				communityId = strCommunityId;
			}
			else if (parameters.TryGetValue("id", out CommunityId justCommunityId))
			{
				communityId = justCommunityId;
			}


			if (communityId == null)
            {
				CommunityId = null;
				CommunityName = null;
				FollowContext = CommunityFollowContext.Default;
				return;
            }

			CommunityId = communityId;

			try
			{
				var res = await CommunityProvider.GetCommunityInfo(CommunityId.Value);

				CommunityName = res.Community.Name;

				CommunityVideoPlaylist = new CommunityVideoPlaylist(CommunityId.Value, new PlaylistId() { Id = communityId, Origin = PlaylistItemsSourceOrigin.CommunityVideos }, res.Community.Name, CommunityProvider);
				SelectedSortOption = CommunityVideoPlaylist.DefaultSortOption;

				this.ObserveProperty(x => x.SelectedSortOption)
					.Subscribe(_ => ResetList())
					.AddTo(_navigationDisposables);
			}
			catch
			{
				Debug.WriteLine("コミュ情報取得に失敗");
			}

			try
			{
				FollowContext = CommunityFollowContext.Default;
				var authority = await _communityFollowProvider.GetCommunityAuthorityAsync(CommunityId.Value);
				if (!authority.Data.IsOwner)
				{
					FollowContext = await CommunityFollowContext.CreateAsync(_communityFollowProvider, new CommunityViewModel() { CommunityId = CommunityId.Value, Name = CommunityName });
				}
			}
			catch
			{
				FollowContext = CommunityFollowContext.Default;
			}

			await base.OnNavigatedToAsync(parameters);
        }



		protected override (int, IIncrementalSource<CommunityVideoInfoViewModel>) GenerateIncrementalSource()
		{
			return (CommunityVideoIncrementalSource.OneTimeLoadCount, new CommunityVideoIncrementalSource(CommunityId, 1, CommunityVideoPlaylist, SelectedSortOption, CommunityProvider, _logger));
		}

        private RelayCommand _OpenCommunityPageCommand;
        private readonly CommunityFollowProvider _communityFollowProvider;

        public RelayCommand OpenCommunityPageCommand
		{
			get
			{
				return _OpenCommunityPageCommand
					?? (_OpenCommunityPageCommand = new RelayCommand(() => 
					{
						PageManager.OpenPageWithId(HohoemaPageType.Community, CommunityId);
					}));
			}
		}

		public ApplicationLayoutManager ApplicationLayoutManager { get; }
		public CommunityProvider CommunityProvider { get; }
        public PageManager PageManager { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    }


	public class CommunityVideoIncrementalSource : IIncrementalSource<CommunityVideoInfoViewModel>
	{
        public CommunityProvider CommunityProvider { get; }

		public string CommunityId { get; private set; }
		public int VideoCount { get; private set; }

		public CommunityVideoIncrementalSource(
			string communityId, 
			int videoCount, 
			CommunityVideoPlaylist communityVideoPlaylist, 
			CommunityVideoPlaylistSortOption sortOption, 
			CommunityProvider communityProvider,
			ILogger logger
			)
		{
            CommunityProvider = communityProvider;
            _logger = logger;
            CommunityId = communityId;
			VideoCount = videoCount;
            _communityVideoPlaylist = communityVideoPlaylist;
            _sortOption = sortOption;
        }

		public const int OneTimeLoadCount = 20;
        private readonly CommunityVideoPlaylist _communityVideoPlaylist;
        private readonly CommunityVideoPlaylistSortOption _sortOption;
        private readonly ILogger _logger;

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

				return itemsRes.Data.Videos.Select((x, i) => new CommunityVideoInfoViewModel(x) { PlaylistItemToken = new PlaylistItemToken(_communityVideoPlaylist, _sortOption, new CommunityVideoContent(x))});
			}
            catch (Exception e)
            {
				_logger.ZLogErrorWithPayload(exception:e, CommunityId, "Community video loading error");
				return Enumerable.Empty<CommunityVideoInfoViewModel>();
			}
		}
    }


	
}

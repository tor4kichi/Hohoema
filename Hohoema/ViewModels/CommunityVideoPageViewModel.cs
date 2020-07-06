using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Community;
using Hohoema.Services;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.ViewModels.Player.Commands;

namespace Hohoema.ViewModels
{
    public class CommunityVideoPageViewModel : HohoemaListingPageViewModelBase<CommunityVideoInfoViewModel>, IPinablePage, ITitleUpdatablePage
	{
		Models.Pages.HohoemaPin IPinablePage.GetPin()
		{
			return new Models.Pages.HohoemaPin()
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

		public CommunityVideoPageViewModel(
			ApplicationLayoutManager applicationLayoutManager,
			CommunityProvider communityProvider,
            PageManager pageManager,
			PlayVideoCommand playVideoCommand
			)
        {
			ApplicationLayoutManager = applicationLayoutManager;
			CommunityProvider = communityProvider;
            PageManager = pageManager;
            PlayVideoCommand = playVideoCommand;
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
                    CommunityDetail = res;

                    CommunityName = CommunityDetail.Name;
                }
                catch
                {
                    Debug.WriteLine("コミュ情報取得に失敗");
                }
            }

            await base.OnNavigatedToAsync(parameters);
        }



		protected override IIncrementalSource<CommunityVideoInfoViewModel> GenerateIncrementalSource()
		{
			return new CommunityVideoIncrementalSource(CommunityId, (int)CommunityDetail.VideoCount, CommunityProvider);
		}

        private DelegateCommand _OpenCommunityPageCommand;
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
        public PlayVideoCommand PlayVideoCommand { get; }
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

		protected override Task<int> ResetSourceImpl()
		{
			Items = new List<RssVideoData>();
			return Task.FromResult(VideoCount);
		}

		protected override async IAsyncEnumerable<CommunityVideoInfoViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			if (count >= VideoCount)
			{
				yield break;
			}

			var tail = (head + count);
			while (Items.Count < tail)
			{
				try
				{
					var pageCount = (uint)(head / 20) + 1;

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
			}

            foreach (var item in Items.Skip(head).Take(count).Select(x => new CommunityVideoInfoViewModel(x) { }))
            {
				yield return item;
            }
		}


		#endregion
	}


	
}

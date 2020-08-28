using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using System.Diagnostics;
using Mntone.Nico2.Videos.Ranking;
using Prism.Commands;
using System.Windows.Input;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using I18NPortable;
using NicoPlayerHohoema.UseCase;
using System.Runtime.CompilerServices;

namespace NicoPlayerHohoema.ViewModels
{
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

		public CommunityVideoPageViewModel(
			ApplicationLayoutManager applicationLayoutManager,
			CommunityProvider communityProvider,
            Services.PageManager pageManager
			)
        {
			ApplicationLayoutManager = applicationLayoutManager;
			CommunityProvider = communityProvider;
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
				yield return new CommunityVideoInfoViewModel(item);

				ct.ThrowIfCancellationRequested();
			}
		}


		#endregion
	}


	
}

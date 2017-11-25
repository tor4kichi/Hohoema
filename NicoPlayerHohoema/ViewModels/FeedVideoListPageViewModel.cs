using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Models;
using System.Diagnostics;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Prism.Commands;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedVideoListPageViewModel : HohoemaVideoListingPageViewModelBase<FeedVideoInfoControlViewModel>
	{
		public Database.Feed FeedGroup { get; private set; }


		public FeedVideoListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn: true)
		{
			LastUpdate = new ReactiveProperty<DateTime>();

			OpenFeedGroupPageCommand = new DelegateCommand(() => 
			{
				if (FeedGroup != null)
				{
					PageManager.OpenPage(HohoemaPageType.FeedGroup, FeedGroup.Id);
				}
			});
		}

		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is int)
			{
				var feedGroupId = (int)e.Parameter;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}
            else if (e.Parameter is string)
            {
                if (int.TryParse(e.Parameter as string, out var feedGroupId))
                {
                    FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
                }
            }

			if (FeedGroup == null)
			{
				// 削除済み？
				PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
			}
			else
			{
				UpdateTitle(FeedGroup.Label);
			}

			await Task.CompletedTask;
		}

        protected override void PostResetList()
		{
			LastUpdate.Value = FeedGroup.UpdateAt;

			base.PostResetList();
		}

		protected override IIncrementalSource<FeedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FeedVideoIncrementalSource(FeedGroup, HohoemaApp.FeedManager);
		}


		public ReactiveProperty<DateTime> LastUpdate { get; private set; }

		public DelegateCommand OpenFeedGroupPageCommand { get; private set; }

	}


	public class FeedVideoIncrementalSource : HohoemaIncrementalSourceBase<FeedVideoInfoControlViewModel>
	{
        FeedManager _FeedManager;
        Database.Feed _FeedGroup;

        List<Tuple<Database.NicoVideo, Database.Bookmark>> _Items;

        public FeedVideoIncrementalSource(Database.Feed feedGroup, FeedManager feedManager)
			: base()
		{
			_FeedGroup = feedGroup;
            _FeedManager = feedManager;
		}

        

        protected override Task<IAsyncEnumerable<FeedVideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(_Items.Skip(head).Take(count).Select(x => 
            {
//                nicoVideo.PreSetTitle(x.Title);
//                nicoVideo.PreSetPostAt(x.SubmitDate);

                var vm = new FeedVideoInfoControlViewModel(x.Item1, x.Item2);
                
                return vm;
            })
            .ToAsyncEnumerable()
            );
        }

        protected override async Task<int> ResetSourceImpl()
        {
            _Items = await _FeedManager.RefreshFeedItemsAsync(_FeedGroup);

            return _Items.Count;
        }
    }
}

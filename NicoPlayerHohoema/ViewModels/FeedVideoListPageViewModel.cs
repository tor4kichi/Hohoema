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
		public IFeedGroup FeedGroup { get; private set; }


		public FeedVideoListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn: true)
		{
			LastUpdate = new ReactiveProperty<DateTime>();

			AllMarkAsReadCommand = new DelegateCommand(async () =>
			{
				FeedGroup.ForceMarkAsRead();

				await HohoemaApp.FeedManager.SaveOne(FeedGroup);

				AllMarkAsReadCommand.RaiseCanExecuteChanged();
			}
			, () =>
			{
				return (FeedGroup?.GetUnreadItemCount() ?? 0 ) > 0;
			});


			SelectedItemsMarkAsReadCommand = SelectedItems.ToCollectionChanged()
				.Select(x => SelectedItems.Count(y => y.IsUnread.Value) > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			SelectedItemsMarkAsReadCommand.Subscribe(async _ =>
			{
				foreach (var item in SelectedItems)
				{
					await HohoemaApp.FeedManager.MarkAsRead(item.Id);
					await HohoemaApp.FeedManager.MarkAsRead(item.RawVideoId);
				}

				ClearSelection();
			})
			.AddTo(_CompositeDisposable);

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
			if (e.Parameter is Guid)
			{
				var feedGroupId = (Guid)e.Parameter;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}
			else if (e.Parameter is string)
			{
				var feedGroupId = Guid.Parse(e.Parameter as string);

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(feedGroupId);
			}

			if (FeedGroup == null)
			{
				// 削除済み？
				PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
			}
			else
			{
				UpdateTitle(FeedGroup.Label);

                if (FeedGroup.IsRefreshRequired)
                {
                    await FeedGroup.Refresh();
                }

                LastUpdate.Value = FeedGroup.UpdateTime;

				AllMarkAsReadCommand.RaiseCanExecuteChanged();
			}

			await Task.CompletedTask;
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (FeedGroup?.IsRefreshRequired ?? false)
			{
				return true;
			}

			return base.CheckNeedUpdateOnNavigateTo(mode);
		}

		protected override void PostResetList()
		{
			LastUpdate.Value = FeedGroup.UpdateTime;

			base.PostResetList();
		}

		protected override IIncrementalSource<FeedVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new FeedVideoIncrementalSource(FeedGroup, HohoemaApp, PageManager);
		}


		public ReactiveProperty<DateTime> LastUpdate { get; private set; }

		public DelegateCommand AllMarkAsReadCommand { get; private set; }
		public ReactiveCommand SelectedItemsMarkAsReadCommand { get; private set; }

		public DelegateCommand OpenFeedGroupPageCommand { get; private set; }

	}


	public class FeedVideoIncrementalSource : HohoemaIncrementalSourceBase<FeedVideoInfoControlViewModel>
	{
		FeedManager _FavFeedManager;
		NiconicoMediaManager _NiconicoMediaManager;
        HohoemaApp _HohoemaApp;
        PageManager _PageManager;
		IFeedGroup _FeedGroup;


		public FeedVideoIncrementalSource(IFeedGroup feedGroup, HohoemaApp hohoemaApp, PageManager pageManager)
			: base()
		{
			_FeedGroup = feedGroup;
			_FavFeedManager = hohoemaApp.FeedManager;
			_NiconicoMediaManager = hohoemaApp.MediaManager;
            _HohoemaApp = hohoemaApp;
            _PageManager = pageManager;
		}



        protected override Task<IAsyncEnumerable<FeedVideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(_FeedGroup.FeedItems.Skip(head).Take(count).Select(x => 
            {
                var nicoVideo = _HohoemaApp.MediaManager.GetNicoVideo(x.VideoId);
                nicoVideo.PreSetTitle(x.Title);
                nicoVideo.PreSetPostAt(x.SubmitDate);

                return new FeedVideoInfoControlViewModel(x, _FeedGroup, nicoVideo, _PageManager);
            })
            .ToAsyncEnumerable()
            );
        }

        protected override async Task<int> ResetSourceImpl()
        {
            await _FeedGroup.Refresh();

            return _FeedGroup.FeedItems.Count;
        }
    }
}

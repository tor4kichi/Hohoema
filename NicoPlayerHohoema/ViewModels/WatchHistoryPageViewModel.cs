using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Collections.ObjectModel;
using NicoPlayerHohoema.Models.Helpers;
using Mntone.Nico2.Videos.Histories;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Async;
using NicoPlayerHohoema.Models.Provider;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.ViewModels
{
	public class WatchHistoryPageViewModel : HohoemaVideoListingPageViewModelBase<HistoryVideoInfoControlViewModel>
	{
		HistoriesResponse _HistoriesResponse;


		public WatchHistoryPageViewModel(
            LoginUserHistoryProvider loginUserHistoryProvider,
            Services.HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager
            )
            : base(pageManager)
		{
			RemoveHistoryCommand = SelectedItems.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			RemoveHistoryCommand.Subscribe(async _ => 
			{
				var selectedItems = SelectedItems.ToArray();

				var action = AsyncInfo.Run<uint>(async (cancelToken, progress) => 
				{
					foreach (var item in selectedItems)
					{
						await RemoveHistory(item.RawVideoId);

						SelectedItems.Remove(item);

						await Task.Delay(250);
					}

					await UpdateList();

					_HistoriesResponse = await LoginUserHistoryProvider.GetHistory();

					RemoveAllHistoryCommand.RaiseCanExecuteChanged();
				});

				await PageManager.StartNoUIWork("視聴履歴の削除", selectedItems.Length, () => action);
			})
			.AddTo(_CompositeDisposable);
            LoginUserHistoryProvider = loginUserHistoryProvider;
            HohoemaPlaylist = hohoemaPlaylist;
        }

		public ReactiveCommand RemoveHistoryCommand { get; private set; }

		private DelegateCommand _RemoveAllHistoryCommand;
		public DelegateCommand RemoveAllHistoryCommand
		{
			get
			{
				return _RemoveAllHistoryCommand
					?? (_RemoveAllHistoryCommand = new DelegateCommand(async () =>
					{
						await RemoveAllHistory();
					}
					, () => MaxItemsCount.Value > 0
					));
			}
		}

        public LoginUserHistoryProvider LoginUserHistoryProvider { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }

        protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			_HistoriesResponse = await LoginUserHistoryProvider.GetHistory();

			await base.ListPageNavigatedToAsync(cancelToken, e, viewModelState);
		}

		protected override IIncrementalSource<HistoryVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new HistoryIncrementalLoadingSource(_HistoriesResponse);
		}

		protected override void PostResetList()
		{
			RemoveAllHistoryCommand.RaiseCanExecuteChanged();

			base.PostResetList();
		}

		internal async Task RemoveAllHistory()
		{
			var action = AsyncInfo.Run(async (cancelToken) => 
			{
				await LoginUserHistoryProvider.RemoveAllHistoriesAsync(_HistoriesResponse.Token);

				_HistoriesResponse = await LoginUserHistoryProvider.GetHistory();

				RemoveAllHistoryCommand.RaiseCanExecuteChanged();
			});

			await PageManager.StartNoUIWork("全ての視聴履歴を削除", () => action);

			await ResetList();
		}

		internal async Task RemoveHistory(string videoId)
		{
			await LoginUserHistoryProvider.RemoveHistoryAsync(_HistoriesResponse.Token, videoId);

			var item = IncrementalLoadingItems.SingleOrDefault(x => x.RawVideoId == videoId);
			IncrementalLoadingItems.Remove(item);

//			await UpdateList();
		}

	}


	public class HistoryVideoInfoControlViewModel : VideoInfoControlViewModel
	{
        public string ItemId { get; set; }
		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }

		public HistoryVideoInfoControlViewModel()
        {

        }




    }


	public class HistoryIncrementalLoadingSource : HohoemaIncrementalSourceBase<HistoryVideoInfoControlViewModel>
	{

		HistoriesResponse _HistoriesResponse;

		public HistoryIncrementalLoadingSource(HistoriesResponse historyRes)
		{
			_HistoriesResponse = historyRes;
		}

		public override uint OneTimeLoadCount
		{
			get
			{
				return 10;
			}
		}
        
        protected override Task<IAsyncEnumerable<HistoryVideoInfoControlViewModel>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(_HistoriesResponse.Histories.Skip(head).Take(count).Select(x => 
            {
                var vm = App.Current.Container.Resolve<HistoryVideoInfoControlViewModel>();
                vm.RawVideoId = x.Id;
                vm.ItemId = x.ItemId;
                vm.LastWatchedAt = x.WatchedAt.DateTime;
                vm.UserViewCount = x.WatchCount;

                vm.SetTitle(x.Title);
                vm.SetThumbnailImage(x.ThumbnailUrl.OriginalString);
                vm.SetVideoDuration(x.Length);
                
                return vm;
            })
            .ToAsyncEnumerable()
            );
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult(_HistoriesResponse?.Histories.Count ?? 0);
        }
    }
}

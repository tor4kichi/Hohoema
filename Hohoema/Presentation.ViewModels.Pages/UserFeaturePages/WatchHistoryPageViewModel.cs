using Hohoema.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Mntone.Nico2.Videos.Histories;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading;
using Prism.Navigation;
using Hohoema.Presentation.Services.Page;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;

namespace Hohoema.Presentation.ViewModels.Pages.UserFeaturePages
{
	public class WatchHistoryPageViewModel : HohoemaViewModelBase
	{
		public WatchHistoryPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            NiconicoSession niconicoSession,
            WatchHistoryManager watchHistoryManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            WatchHistoryRemoveAllCommand watchHistoryRemoveAllCommand
            )
		{
            ApplicationLayoutManager = applicationLayoutManager;
            _niconicoSession = niconicoSession;
            _watchHistoryManager = watchHistoryManager;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            WatchHistoryRemoveAllCommand = watchHistoryRemoveAllCommand;
            Histories = new ObservableCollection<HistoryVideoInfoControlViewModel>();
        }

        private readonly NiconicoSession _niconicoSession;
        private readonly WatchHistoryManager _watchHistoryManager;

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public WatchHistoryRemoveAllCommand WatchHistoryRemoveAllCommand { get; }
        public ObservableCollection<HistoryVideoInfoControlViewModel> Histories { get; }

        HistoriesResponse _HistoriesResponse;


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (RefreshCommand.CanExecute())
            {
                RefreshCommand.Execute();
            }


            Observable.FromEventPattern<WatchHistoryRemovedEventArgs>(
                h => _watchHistoryManager.WatchHistoryRemoved += h,
                h => _watchHistoryManager.WatchHistoryRemoved -= h
                )
                .Subscribe(e =>
                {
                    var args = e.EventArgs;
                    var removedItem = Histories.FirstOrDefault(x => x.ItemId == args.ItemId);
                    if (removedItem != null)
                    {
                        Histories.Remove(removedItem);
                    }
                })
                .AddTo(_CompositeDisposable);

            Observable.FromEventPattern(
                h => _watchHistoryManager.WatchHistoryAllRemoved += h,
                h => _watchHistoryManager.WatchHistoryAllRemoved -= h
                )
                .Subscribe(_ =>
                {
                    Histories.Clear();
                })
                .AddTo(_CompositeDisposable);

            base.OnNavigatedTo(parameters);
        }

        private DelegateCommand _RefreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                return _RefreshCommand
                    ?? (_RefreshCommand = new DelegateCommand(async () =>
                    {
                        Histories.Clear();

                        _HistoriesResponse = await _watchHistoryManager.GetWatchHistoriesAsync();

                        foreach (var x in _HistoriesResponse?.Histories ?? Enumerable.Empty<History>())
                        {
                            var vm = new HistoryVideoInfoControlViewModel(x.Id);
                            vm.ItemId = x.ItemId;
                            vm.LastWatchedAt = x.WatchedAt.DateTime;
                            vm.UserViewCount = x.WatchCount;

                            vm.SetTitle(x.Title);
                            vm.SetThumbnailImage(x.ThumbnailUrl.OriginalString);
                            vm.SetVideoDuration(x.Length);

                            vm.RemoveToken = _HistoriesResponse.Token;

                            _ = vm.InitializeAsync(default);

                            Histories.Add(vm);
                        }
                    }
                    , () => _niconicoSession.IsLoggedIn
                    ));
            }
        }



    }

    


    public class HistoryVideoInfoControlViewModel : VideoInfoControlViewModel, IWatchHistory
    {
        public string RemoveToken { get; set; }

        public string ItemId { get; set; }
		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }

		public HistoryVideoInfoControlViewModel(
            string rawVideoId
            )
            : base(rawVideoId)
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
        
        protected override async IAsyncEnumerable<HistoryVideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var item in _HistoriesResponse.Histories.Skip(head).Take(count))
            {
                var vm = new HistoryVideoInfoControlViewModel(item.Id);
                vm.ItemId = item.ItemId;
                vm.LastWatchedAt = item.WatchedAt.DateTime;
                vm.UserViewCount = item.WatchCount;

                vm.SetTitle(item.Title);
                vm.SetThumbnailImage(item.ThumbnailUrl.OriginalString);
                vm.SetVideoDuration(item.Length);

                _ =  vm.InitializeAsync(ct);
                yield return vm;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult(_HistoriesResponse?.Histories.Count ?? 0);
        }
    }
}

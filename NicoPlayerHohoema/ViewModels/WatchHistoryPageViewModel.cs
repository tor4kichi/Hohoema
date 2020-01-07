using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Unity;
using Prism.Navigation;
using Prism.Unity;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;

namespace NicoPlayerHohoema.ViewModels
{
	public class WatchHistoryPageViewModel : HohoemaViewModelBase
	{
		public WatchHistoryPageViewModel(
            LoginUserHistoryProvider loginUserHistoryProvider,
            HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager
            )
		{
            LoginUserHistoryProvider = loginUserHistoryProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            Histories = new ObservableCollection<HistoryVideoInfoControlViewModel>();

            Histories.ObserveElementPropertyChanged()
                .Where(x => x.EventArgs.PropertyName == nameof(HistoryVideoInfoControlViewModel.IsRemoved))
                .Where(x => x.Sender.IsRemoved)
                .Subscribe(x => Histories.Remove(x.Sender))
                .AddTo(_CompositeDisposable);
        }


        public LoginUserHistoryProvider LoginUserHistoryProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.PageManager PageManager { get; }
        public ObservableCollection<HistoryVideoInfoControlViewModel> Histories { get; }

        HistoriesResponse _HistoriesResponse;


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (RefreshCommand.CanExecute())
            {
                RefreshCommand.Execute();
            }

            base.OnNavigatedTo(parameters);
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = null;
            return false;
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

                        _HistoriesResponse = await LoginUserHistoryProvider.GetHistory();

                        foreach (var x in _HistoriesResponse.Histories ?? Enumerable.Empty<History>())
                        {
                            var vm = new HistoryVideoInfoControlViewModel(x.Id);
                            vm.ItemId = x.ItemId;
                            vm.LastWatchedAt = x.WatchedAt.DateTime;
                            vm.UserViewCount = x.WatchCount;

                            vm.SetTitle(x.Title);
                            vm.SetThumbnailImage(x.ThumbnailUrl.OriginalString);
                            vm.SetVideoDuration(x.Length);

                            vm.RemoveToken = _HistoriesResponse.Token;

                            Histories.Add(vm);
                        }

                        RemoveAllHistoryCommand.RaiseCanExecuteChanged();
                    }
                    , () => LoginUserHistoryProvider.NiconicoSession.IsLoggedIn
                    ));
            }
        }

        private DelegateCommand _RemoveAllHistoryCommand;
        public DelegateCommand RemoveAllHistoryCommand
        {
            get
            {
                return _RemoveAllHistoryCommand
                    ?? (_RemoveAllHistoryCommand = new DelegateCommand(async () =>
                    {
                        await LoginUserHistoryProvider.RemoveAllHistoriesAsync(_HistoriesResponse.Token);

                        _HistoriesResponse = await LoginUserHistoryProvider.GetHistory();

                        Histories.Clear();

                        RemoveAllHistoryCommand.RaiseCanExecuteChanged();
                    }
                    , () => Histories.Count > 0
                    ));
            }
        }

    }


    public class HistoryVideoInfoControlViewModel : VideoInfoControlViewModel
	{
        public string RemoveToken { get; set; }

        public string ItemId { get; set; }
		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }


        public bool IsRemoved { get; private set; }

		public HistoryVideoInfoControlViewModel(
            string rawVideoId
            )
            : base(rawVideoId)
        {
            
        }

        private DelegateCommand _RemoveHistoryCommand;
        public DelegateCommand RemoveHistoryCommand => _RemoveHistoryCommand
            ?? (_RemoveHistoryCommand = new DelegateCommand(() => 
            {
                var provider = App.Current.Container.Resolve<LoginUserHistoryProvider>();
                _ = provider.RemoveHistoryAsync(RemoveToken, ItemId);

                IsRemoved = true;
                RaisePropertyChanged(nameof(IsRemoved));
            }));

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
                var vm = new HistoryVideoInfoControlViewModel(x.Id);
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

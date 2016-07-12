using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Collections.ObjectModel;
using NicoPlayerHohoema.Util;
using Mntone.Nico2.Videos.Histories;

namespace NicoPlayerHohoema.ViewModels
{
	public class HistoryPageViewModel : HohoemaVideoListingPageViewModelBase<HistoryVideoInfoControlViewModel>
	{
		public HistoryPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{			
			base.OnNavigatedTo(e, viewModelState);
		}

		public override string GetPageTitle()
		{
			return "視聴履歴";
		}

		protected override uint IncrementalLoadCount
		{
			get
			{
				return 20;
			}
		}

		protected override IIncrementalSource<HistoryVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new HistoryIncrementalLoadingSource(HohoemaApp, PageManager);
		}

		protected override bool CheckNeedUpdate()
		{
			return true;
		}

		
	}


	public class HistoryVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public HistoryVideoInfoControlViewModel(uint viewCount, NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			UserViewCount = viewCount;
		}

		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }
	}


	public class HistoryIncrementalLoadingSource : IIncrementalSource<HistoryVideoInfoControlViewModel>
	{
		public HistoryIncrementalLoadingSource(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
		}

		public async Task<IEnumerable<HistoryVideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			if (_HistoriesResponse == null)
			{
				_HistoriesResponse = await _HohoemaApp.ContentFinder.GetHistory();
			}

			var head = (int)pageIndex - 1;
			var list = new List<HistoryVideoInfoControlViewModel>();
			foreach (var history in _HistoriesResponse.Histories.Skip(head).Take((int)pageSize))
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(history.Id);
				var vm = new HistoryVideoInfoControlViewModel(
					history.WatchCount
					, nicoVideo
					, _PageManager
					);

				vm.LastWatchedAt = history.WatchedAt.DateTime;
				vm.MovieLength = history.Length;
				vm.ThumbnailImageUrl = history.ThumbnailUrl;

				list.Add(vm);
			}

			return list;
		}


		HistoriesResponse _HistoriesResponse;

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;

	}
}

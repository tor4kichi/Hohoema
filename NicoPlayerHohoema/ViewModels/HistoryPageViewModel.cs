﻿using NicoPlayerHohoema.Models;
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
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class HistoryPageViewModel : HohoemaVideoListingPageViewModelBase<HistoryVideoInfoControlViewModel>
	{
		HistoriesResponse _HistoriesResponse;


		public HistoryPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
			: base(hohoemaApp, pageManager, mylistDialogService)
		{
			RemoveHistoryCommand = SelectedVideoInfoItems.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			RemoveHistoryCommand.Subscribe(async _ => 
			{
				var selectedItems = SelectedVideoInfoItems.ToArray();

				foreach (var item in selectedItems)
				{
					await RemoveHistory(item.RawVideoId);

					SelectedVideoInfoItems.Remove(item);

					await Task.Delay(250);
				}

				await UpdateList();

				_HistoriesResponse = await HohoemaApp.ContentFinder.GetHistory();

				RemoveAllHistoryCommand.RaiseCanExecuteChanged();
			})
			.AddTo(_CompositeDisposable);
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

		protected override uint IncrementalLoadCount
		{
			get
			{
				return 20;
			}
		}


		
		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			_HistoriesResponse = await HohoemaApp.ContentFinder.GetHistory();

			await base.ListPageNavigatedToAsync(cancelToken, e, viewModelState);
		}

		protected override IIncrementalSource<HistoryVideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new HistoryIncrementalLoadingSource(HohoemaApp, PageManager, _HistoriesResponse);
		}

		protected override void PostResetList()
		{
			RemoveAllHistoryCommand.RaiseCanExecuteChanged();

			base.PostResetList();
		}

		internal async Task RemoveAllHistory()
		{
			await HohoemaApp.NiconicoContext.Video.RemoveAllHistoriesAsync(_HistoriesResponse.Token);

			RemoveAllHistoryCommand.RaiseCanExecuteChanged();

			await ResetList();
		}

		internal async Task RemoveHistory(string videoId)
		{
			await HohoemaApp.NiconicoContext.Video.RemoveHistoryAsync(_HistoriesResponse.Token, videoId);

			var item = IncrementalLoadingItems.SingleOrDefault(x => x.RawVideoId == videoId);
			IncrementalLoadingItems.Remove(item);

//			await UpdateList();
		}

	}


	public class HistoryVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public DateTime LastWatchedAt { get; set; }
		public uint UserViewCount { get; set; }

		public HistoryVideoInfoControlViewModel(uint viewCount, NicoVideo nicoVideo, PageManager pageManager)
			: base(nicoVideo, pageManager)
		{
			UserViewCount = viewCount;
		}



		
	}


	public class HistoryIncrementalLoadingSource : IIncrementalSource<HistoryVideoInfoControlViewModel>
	{
		public HistoryIncrementalLoadingSource(HohoemaApp hohoemaApp, PageManager pageManager, HistoriesResponse historyRes)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			_HistoriesResponse = historyRes;
		}


		public Task<int> ResetSource()
		{
			return Task.FromResult(_HistoriesResponse.Histories.Count);
		}


		public async Task<IEnumerable<HistoryVideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			
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


			foreach (var item in list)
			{
				await item.LoadThumbnail();
			}

			return list;
		}

		HistoriesResponse _HistoriesResponse;
	
		HohoemaApp _HohoemaApp;
		PageManager _PageManager;

	}
}

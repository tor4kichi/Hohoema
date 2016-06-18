using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class CacheManagementPageViewModel : HohoemaViewModelBase
	{
		public CacheManagementPageViewModel(HohoemaApp app, PageManager pageManager)
			: base(pageManager)
		{
			_HohoemaApp = app;
			_MediaManager = app.MediaManager;

			_CacheVideoVMs = new Dictionary<NicoVideoCacheRequest, CacheVideoViewModel>();

			CacheRequestItems = _MediaManager.Context.CacheRequestStack
				.ToReadOnlyReactiveCollection(x =>
				{
					var task = ToCacheVideoViewModel(x);
					task.Wait();
					return task.Result;
				});

			CurrentDownloadItem = _MediaManager.Context.ObserveProperty(x => x.CurrentDownloadStream)
				.Select(x =>
				{
					if (x == null) { return CacheProgressVideoViewModel.Empty; }

					var task = _MediaManager.GetNicoVideo(x.RawVideoId);
					task.Wait();


					return new CacheProgressVideoViewModel(task.Result, x);
				})
				.ToReadOnlyReactiveProperty(CacheProgressVideoViewModel.Empty);

		}

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			await this.UpdateLocalCacheFilesVM();
		}

		private async Task<CacheVideoViewModel> ToCacheVideoViewModel(string videoId, NicoVideoQuality quality)
		{
			return await ToCacheVideoViewModel(new NicoVideoCacheRequest()
			{
				RawVideoid = videoId,
				Quality = quality
			});
		}

		private async Task<CacheVideoViewModel> ToCacheVideoViewModel(NicoVideoCacheRequest req)
		{
			if (_CacheVideoVMs.ContainsKey(req))
			{
				return _CacheVideoVMs[req];
			}
			else
			{
				var nicoVideo = await _MediaManager.GetNicoVideo(req.RawVideoid).ConfigureAwait(false);
				var vm = new CacheVideoViewModel(nicoVideo, req.Quality);
				_CacheVideoVMs.Add(req, vm);
				return vm;
			}
		}

		private async Task UpdateLocalCacheFilesVM()
		{
			var saveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

			// videoフォルダから *_info.json のファイルを取得してNicoVideoオブジェクトを初期化
			var files = await saveFolder.GetFilesAsync();
			var videoIds = files
				.Where(x => x.Name.EndsWith("_info.json"))
				.OrderBy(x => x.DateCreated)
				.Select(x => new String(x.Name.TakeWhile(y => y != '_').ToArray()));

			foreach (var videoId in videoIds)
			{
				var nicoVideo = await _MediaManager.GetNicoVideo(videoId).ConfigureAwait(false);

				
				if (NicoVideoCachedStream.ExistLowQuorityVideo(nicoVideo.CachedWatchApiResponse, saveFolder))
				{
					var vm = await ToCacheVideoViewModel(videoId, NicoVideoQuality.Low).ConfigureAwait(false);
					if (!LocalCachedItems.Contains(vm))
					{
						LocalCachedItems.Insert(0, vm);
					}

				}
				

				if (NicoVideoCachedStream.ExistOriginalQuorityVideo(nicoVideo.CachedWatchApiResponse, saveFolder))
				{
					var vm = await ToCacheVideoViewModel(videoId, NicoVideoQuality.Original).ConfigureAwait(false);
					if (!LocalCachedItems.Contains(vm))
					{
						LocalCachedItems.Insert(0, vm);
					}
				}

				
			}


			


		}

		

		public override string GetPageTitle()
		{
			return "ダウンロード管理";
		}


		HohoemaApp _HohoemaApp;
		NiconicoMediaManager _MediaManager;

		private Dictionary<NicoVideoCacheRequest, CacheVideoViewModel> _CacheVideoVMs;

		/// <summary>
		/// 現在キャッシュ処理中のアイテム
		/// </summary>
		public ReadOnlyReactiveProperty<CacheProgressVideoViewModel> CurrentDownloadItem { get; private set; }

		/// <summary>
		/// キャッシュをリクエストされたアイテム
		/// </summary>
		public ReadOnlyReactiveCollection<CacheVideoViewModel> CacheRequestItems { get; private set; }

		/// <summary>
		/// キャッシュ未完了のアイテムも含むキャッシュファイル
		/// </summary>
		public ObservableCollection<CacheVideoViewModel> LocalCachedItems { get; private set; }
	}

	// 自身のキャッシュ状況を表現する
	// キャッシュ処理の状態、進捗状況
	
	public class CacheProgressVideoViewModel : CacheVideoViewModel
	{
		public static CacheProgressVideoViewModel Empty { get; private set; } = new CacheProgressVideoViewModel();

		private CacheProgressVideoViewModel()
		{

		}

		public CacheProgressVideoViewModel(NicoVideo nicoVideo, NicoVideoCachedStream stream)
			: base(nicoVideo, stream.Quality)
		{
			_Stream = stream;
			_ProgressMonitorTimerDisposeHandle = Observable.Timer(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), UIDispatcherScheduler.Default)
				.Subscribe(x => 
				{
					if (_Stream.IsCacheComplete)
					{
						ProgressPercent = 100.0f;
					}
					else if (_Stream.Progress != null)
					{
						var totalSize = _Stream.Progress.Size;
						var size = _Stream.Progress.RemainSize();

						if (totalSize == 0) { throw new Exception("CacheProgress can not display progress percentage, due to Stream size is Zero."); }

						// 少数第一位までを計算する
						ProgressPercent = 100.0f - (float)Math.Floor((size  / (float)totalSize) * 1000.0f) * 0.1f;
					}
				});
		}

		public override void Dispose()
		{
			base.Dispose();

			_ProgressMonitorTimerDisposeHandle?.Dispose();
		}

		private float _ProgressPercent;
		public float ProgressPercent
		{
			get { return _ProgressPercent; }
			set { SetProperty(ref _ProgressPercent, value); }
		}


		IDisposable _ProgressMonitorTimerDisposeHandle;
		NicoVideoCachedStream _Stream;
	}


	public class CacheVideoViewModel : BindableBase, IDisposable
	{
		internal CacheVideoViewModel()
		{
			RealVideoId = "";
			Title = "";
			VideoId = "";
		}

		public CacheVideoViewModel(NicoVideo nicoVideo, NicoVideoQuality quality)
		{
			VideoId = nicoVideo.RawVideoId;
			Quality = quality;
			Title = nicoVideo.CachedWatchApiResponse.videoDetail.title;


		}

		public bool IsIncompleteCache { get; private set; }

		public string RealVideoId { get; private set; }
		public string Title { get; private set; }

		public string VideoId { get; private set; }
		public NicoVideoQuality Quality { get; private set; }

		public virtual void Dispose()
		{
			
		}
	}

}

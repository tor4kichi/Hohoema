using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.Networking.BackgroundTransfer;
using System.Text.RegularExpressions;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
    public delegate void VideoCacheStateChangedEventHandler(object sender, NicoVideoCacheRequest request, NicoVideoCacheState state);
    
    /// <summary>
    /// ニコニコ動画の動画やサムネイル画像、
    /// 動画情報など動画に関わるメディアを管理します
    /// </summary>
    public class NiconicoMediaManager : BindableBase, IDisposable, IBackgroundUpdateable
	{
        HohoemaApp _HohoemaApp;
        public bool IsInitialized { get; private set; }


        private static readonly Regex NicoVideoIdRegex = new Regex("(?:(?:sm|so|lv)\\d*)");

        public VideoDownloadManager VideoDownloadManager { get; private set; }


        private AsyncLock _CacheVideosLock = new AsyncLock();
        private ObservableCollection<NicoVideo> _CacheVideos;
        public ReadOnlyObservableCollection<NicoVideo> CacheVideos { get; private set; }


        private AsyncLock _VideoIdToNicoVideoLock = new AsyncLock();
        private Dictionary<string, NicoVideo> _VideoIdToNicoVideo;



        public event VideoCacheStateChangedEventHandler VideoCacheStateChanged;


        public static NicoVideoCacheRequest CacheRequestInfoFromFileName(IStorageFile file)
        {
            // キャッシュリクエストを削除
            // 2重に拡張子を利用しているので二回GetFileNameWithoutExtensionを掛けることでIDを取得
            var match = NicoVideoIdRegex.Match(file.Name);
            var id = match.Value;
            var quality = NicoVideoQualityFileNameHelper.NicoVideoQualityFromFileNameExtention(file.Name);

            return new NicoVideoCacheRequest() { RawVideoId = id, Quality = quality };
        }

        static internal Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);

//            await man.RetrieveCacheCompletedVideos();

            return Task.FromResult(man);
		}



		private NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;
            VideoDownloadManager = new VideoDownloadManager(_HohoemaApp, this);
            VideoDownloadManager.Requested += VideoDownloadManager_Requested;
            VideoDownloadManager.RequestCanceled += VideoDownloadManager_RequestCanceled;
            VideoDownloadManager.DownloadStarted += VideoDownloadManager_DownloadStarted;
            VideoDownloadManager.DownloadCompleted += VideoDownloadManager_DownloadCompleted;
            VideoDownloadManager.DownloadCanceled += VideoDownloadManager_DownloadCanceled;

            _VideoIdToNicoVideo = new Dictionary<string, NicoVideo>();

			_CacheVideos = new ObservableCollection<NicoVideo>();
			CacheVideos = new ReadOnlyObservableCollection<NicoVideo>(_CacheVideos);

            _HohoemaApp.OnSignin += _HohoemaApp_OnSignin;

        }


        private async void VideoDownloadManager_Requested(object sender, NicoVideoCacheRequest request)
        {
            VideoCacheStateChanged?.Invoke(this, request, NicoVideoCacheState.Pending);

            await CacheRequested(request);
        }

        private async void VideoDownloadManager_RequestCanceled(object sender, NicoVideoCacheRequest request)
        {
            VideoCacheStateChanged?.Invoke(this, request, NicoVideoCacheState.NotCacheRequested);

            await CacheRequestCanceled(request);
        }

        private void VideoDownloadManager_DownloadStarted(object sender, NicoVideoCacheRequest request, DownloadOperation op)
        {
            VideoCacheStateChanged?.Invoke(this, request, NicoVideoCacheState.Downloading);
        }

        private async void VideoDownloadManager_DownloadCompleted(object sender, NicoVideoCacheRequest request, string filePath)
        {
            var nicoVideo = await GetNicoVideoAsync(request.RawVideoId);
            var div = nicoVideo.GetDividedQualityNicoVideo(request.Quality);

            await div.RestoreCache(filePath);
            VideoCacheStateChanged?.Invoke(this, request, NicoVideoCacheState.Cached);
        }

        private void VideoDownloadManager_DownloadCanceled(object sender, NicoVideoCacheRequest request)
        {
            VideoCacheStateChanged?.Invoke(this, request, NicoVideoCacheState.Pending);
        }






        private void _HohoemaApp_OnSignin()
		{
			// 初期化をバックグラウンドタスクに登録
			//var updater = 
			//updater.Completed += (sender, item) => 
			//{
//				IsInitialized = true;
			//};
		}

		public void Dispose()
		{
		}


		#region interface IBackgroundUpdateable

		public IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher)
		{
			return Initialize()
				.AsAsyncAction();
		}

		#endregion


		private async Task Initialize()
        {
            // キャッシュ完了したアイテムを検索
            await RetrieveCacheCompletedVideos();

            // ダウンロード中の情報を復元
            await VideoDownloadManager.Initialize();

            IsInitialized = true;
        }


		


		


		public async Task<NicoVideo> GetNicoVideoAsync(string rawVideoId, bool withInitialize = true)
		{
			NicoVideo nicoVideo = null;
			bool isFirstGet = false;
			using (var releaser = await _VideoIdToNicoVideoLock.LockAsync())
			{
				if (false == _VideoIdToNicoVideo.ContainsKey(rawVideoId))
				{
					nicoVideo = new NicoVideo(_HohoemaApp, rawVideoId, _HohoemaApp.MediaManager);
					_VideoIdToNicoVideo.Add(rawVideoId, nicoVideo);
					isFirstGet = true;
				}
				else
				{
					nicoVideo = _VideoIdToNicoVideo[rawVideoId];
				}
			}

			if (isFirstGet && withInitialize)
			{
				using (var releaser = await _VideoIdToNicoVideoLock.LockAsync())
				{
					await nicoVideo.Initialize();
				}
			}

			return nicoVideo;
		}


		public async Task<List<NicoVideo>> GetNicoVideoItemsAsync(params string[] idList)
		{
			List<NicoVideo> videos = new List<NicoVideo>();

			using (var releaser = await _VideoIdToNicoVideoLock.LockAsync())
			{
				foreach (var id in idList)
				{
					NicoVideo nicoVideo = null;
					if (false == _VideoIdToNicoVideo.ContainsKey(id))
					{
						nicoVideo = new NicoVideo(_HohoemaApp, id, _HohoemaApp.MediaManager);
						_VideoIdToNicoVideo.Add(id, nicoVideo);
					}
					else
					{
						nicoVideo = _VideoIdToNicoVideo[id];
					}

					videos.Add(nicoVideo);
				}
			}

			foreach (var video in videos.AsParallel())
			{
				await video.Initialize().ConfigureAwait(false);
			}

			return videos;
		}

        
		private async Task RetrieveCacheCompletedVideos()
		{
			var videoFolder = await _HohoemaApp.GetVideoCacheFolder();
			if (videoFolder != null)
			{
				var files = await videoFolder.GetFilesAsync();

                foreach (var file in files)
                {
                    if (file.FileType != ".mp4")
                    {
                        continue;
                    }

                    // ファイル名の最後方にある[]の中身の文字列を取得
                    // (動画タイトルに[]が含まれる可能性に配慮)
                    var match = NicoVideoIdRegex.Match(file.Name);
                    var id = match.Value;
                    var quality = NicoVideoQualityFileNameHelper.NicoVideoQualityFromFileNameExtention(file.Name);
                    var info = new NicoVideoCacheRequest()
                    {
                        RawVideoId = id,
                        Quality = quality,
                    };

                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

					var nicoVideo = await GetNicoVideoAsync(info.RawVideoId);
                    var div = nicoVideo.GetDividedQualityNicoVideo(quality);

                    await nicoVideo.RestoreCache(quality, file.Path);

                    await CacheRequested(info);

                    VideoCacheStateChanged?.Invoke(this, info, NicoVideoCacheState.Cached);

                    Debug.Write(".");
                }
			}
		}

        public async Task OnCacheFolderChanged()
		{
            StopCacheDownload();

            // TODO: 現在データを破棄して、変更されたフォルダの内容で初期化しなおす
            _CacheVideos.Clear();

            await RetrieveCacheCompletedVideos();
		}

	

        internal void StopCacheDownload()
        {
            // TODO: 
        }


        private async Task CacheRequested(NicoVideoCacheRequest req)
        {
            var nicoVideo = await GetNicoVideoAsync(req.RawVideoId);
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                if (_CacheVideos.Any(x => x.RawVideoId == nicoVideo.RawVideoId))
                {
                    _CacheVideos.Remove(nicoVideo);
                }

                // 新しく追加されるアイテムを先頭に配置
                _CacheVideos.Insert(0, nicoVideo);
            }
        }

        private async Task CacheRequestCanceled(NicoVideoCacheRequest req)
        {
            var nicoVideo = await GetNicoVideoAsync(req.RawVideoId);

            if (nicoVideo.GetAllQuality().All(x => !x.IsCacheRequested))
            {
                using (var releaser = await _CacheVideosLock.LockAsync())
                {
                    _CacheVideos.Remove(nicoVideo);
                }
            }
        }



    }




    public class NicoVideoCacheProgress : NicoVideoCacheRequest
    {
        public DownloadOperation DownloadOperation { get; set; }
    }







}

using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Helpers;
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
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models.Db;
using System.Collections.Concurrent;

namespace NicoPlayerHohoema.Models
{
    public delegate void VideoCacheStateChangedEventHandler(object sender, NicoVideoCacheRequest request, NicoVideoCacheState state);
    
    /// <summary>
    /// ニコニコ動画の動画やサムネイル画像、
    /// 動画情報など動画に関わるメディアを管理します
    /// </summary>
    public class NiconicoMediaManager : AsyncInitialize, IDisposable
	{
        HohoemaApp _HohoemaApp;

        private static readonly Regex NicoVideoIdRegex = new Regex("\\[((?:sm|so|lv)\\d+)\\]");

        public VideoDownloadManager VideoDownloadManager { get; private set; }


        private AsyncLock _CacheVideosLock = new AsyncLock();
        private ObservableCollection<NicoVideo> _CacheVideos;
        public ReadOnlyObservableCollection<NicoVideo> CacheVideos { get; private set; }


        private AsyncLock _VideoIdToNicoVideoLock = new AsyncLock();
        private ConcurrentDictionary<string, NicoVideo> _VideoIdToNicoVideo;


        public event VideoCacheStateChangedEventHandler VideoCacheStateChanged;

        public static NicoVideoCacheRequest CacheRequestInfoFromFileName(IStorageFile file)
        {
            // キャッシュリクエストを削除
            // 2重に拡張子を利用しているので二回GetFileNameWithoutExtensionを掛けることでIDを取得
            var match = NicoVideoIdRegex.Match(file.Name);
            if (match != null)
            {
                var id = match.Groups[1].Value;
                var quality = NicoVideoQualityFileNameHelper.NicoVideoQualityFromFileNameExtention(file.Name);

                return new NicoVideoCacheRequest() { RawVideoId = id, Quality = quality };
            }
            else
            {
                throw new Exception();
            }
        }

        static internal Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);
            man.Initialize();

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

            _VideoIdToNicoVideo = new ConcurrentDictionary<string, NicoVideo>();

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
            var nicoVideo = GetNicoVideo(request.RawVideoId);
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

        protected override Task OnInitializeAsync(CancellationToken token)
        {
            return Windows.System.Threading.ThreadPool.RunAsync(async (workItem) => 
            {
                // キャッシュ完了したアイテムを検索
                await RetrieveCacheCompletedVideos();

                // ダウンロード中の情報を復元
                await VideoDownloadManager.Initialize();
            },
            Windows.System.Threading.WorkItemPriority.Normal
            )
            .AsTask();
        }


        public async Task<NicoVideo> GetNicoVideoAsync(string rawVideoId)
        {
            NicoVideo nicoVideo = _VideoIdToNicoVideo.GetOrAdd(rawVideoId, (id) => new NicoVideo(_HohoemaApp, id, _HohoemaApp.MediaManager));

            await nicoVideo.Initialize();

            return nicoVideo;
        }

        public NicoVideo GetNicoVideo(string rawVideoId)
        {
            NicoVideo nicoVideo = _VideoIdToNicoVideo.GetOrAdd(rawVideoId, (id) => new NicoVideo(_HohoemaApp, id, _HohoemaApp.MediaManager));

            return nicoVideo;
        }


		public List<NicoVideo> GetNicoVideoItems(params string[] idList)
		{
			List<NicoVideo> videos = new List<NicoVideo>();

            foreach (var rawVideoId in idList)
            {
                NicoVideo nicoVideo = _VideoIdToNicoVideo.GetOrAdd(rawVideoId, (id) => new NicoVideo(_HohoemaApp, id, _HohoemaApp.MediaManager));

                nicoVideo.Initialize().ConfigureAwait(false);

                videos.Add(nicoVideo);
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
                    var id = match.Groups[1].Value;
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

					var nicoVideo = GetNicoVideo(info.RawVideoId);
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
            var nicoVideo = GetNicoVideo(req.RawVideoId);
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
            var nicoVideo = GetNicoVideo(req.RawVideoId);

            if (nicoVideo.GetAllQuality().All(x => !x.IsCacheRequested))
            {
                using (var releaser = await _CacheVideosLock.LockAsync())
                {
                    _CacheVideos.Remove(nicoVideo);
                }
            }
        }




        internal async Task NotifyCacheForceDeleted(NicoVideo nicoVideo)
        {
            // キャッシュ登録を削除
            var videoInfo = await VideoInfoDb.GetAsync(nicoVideo.RawVideoId);
            if (videoInfo != null)
            {
                videoInfo.IsDeleted = true;
                await VideoInfoDb.UpdateAsync(videoInfo);
            }

            var toastService = App.Current.Container.Resolve<Views.Service.ToastNotificationService>();
            toastService.ShowText("動画削除：" + nicoVideo.RawVideoId
                , $"{nicoVideo.Title} はニコニコ動画サーバーから削除されたため、キャッシュを強制削除しました。"
                , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                );

        }


    }




    public class NicoVideoCacheProgress : NicoVideoCacheRequest
    {
        public DownloadOperation DownloadOperation { get; set; }
    }







}

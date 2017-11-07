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
    public struct VideoCacheStateChangedEventArgs
    {
        public NicoVideoCacheRequest Request { get; set; }
        public NicoVideoCacheState CacheState { get; set; }
    }
    
    /// <summary>
    /// ニコニコ動画の動画やサムネイル画像、
    /// 動画情報など動画に関わるメディアを管理します
    /// </summary>
    public class VideoCacheManager : AsyncInitialize, IDisposable
	{
        private static readonly Regex NicoVideoIdRegex = new Regex("\\[((?:sm|so|lv)\\d+)\\]");

        HohoemaApp _HohoemaApp;

        private VideoDownloadManager VideoDownloadManager { get; }
        private AsyncLock _CacheVideosLock = new AsyncLock();
        private ConcurrentDictionary<string, List<NicoVideoCacheInfo>> _CacheVideos;

        public event EventHandler<VideoCacheStateChangedEventArgs> VideoCacheStateChanged;


        public async Task<IEnumerable<NicoVideoCacheInfo>> EnumerateCacheVideosAsync()
        {
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                return _CacheVideos.SelectMany(x => x.Value).ToArray();
            }
        }

        public async Task<IEnumerable<NicoVideoCacheRequest>> EnumerateCacheRequestedVideosAsync()
        {
            List<NicoVideoCacheRequest> list = new List<NicoVideoCacheRequest>();

            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                list.AddRange(_CacheVideos.SelectMany(x => x.Value));
            }

            var requestedItems = await VideoDownloadManager.GetCacheRequestedVideosAsync();
            list.AddRange(requestedItems);

            return list;
        }

        public async Task<IEnumerable<NicoVideoCacheRequest>> GetCacheRequest(string videoId)
        {
            var allCacheRequested = await VideoDownloadManager.GetCacheRequestedVideosAsync();
            var cacheRequested = allCacheRequested.Where(x => x.RawVideoId == videoId);
            if (_CacheVideos.TryGetValue(videoId, out var list))
            {
                return cacheRequested.Concat(list);
            }
            else
            {
                return cacheRequested;
            }
        }




        public static string MakeCacheVideoFileName(string title, string videoId, MovieType videoType, NicoVideoQuality quality)
        {
            string toQualityNameExtention;
            var filename = $"{title.ToSafeFilePath()} - [{videoId}]";
            switch (quality)
            {
                case NicoVideoQuality.Smile_Original:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Smile_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".low.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".xlow.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_Midium:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".xmid.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_High:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".xhigh.{videoType.ToString().ToLower()}");
                    break;
                default:
                    throw new NotSupportedException(quality.ToString());
            }

            return toQualityNameExtention;
        }

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

        static internal async Task<VideoCacheManager> Create(HohoemaApp app)
		{
			var man = new VideoCacheManager(app);

            await man.Initialize();
//            await man.RetrieveCacheCompletedVideos();

            return man;
		}



		private VideoCacheManager(HohoemaApp app)
		{
			_HohoemaApp = app;
            VideoDownloadManager = new VideoDownloadManager(_HohoemaApp, this);
            VideoDownloadManager.DownloadStarted += VideoDownloadManager_DownloadStarted;
            VideoDownloadManager.DownloadCompleted += VideoDownloadManager_DownloadCompleted;
            VideoDownloadManager.DownloadCanceled += VideoDownloadManager_DownloadCanceled;

			_CacheVideos = new ConcurrentDictionary<string, List<NicoVideoCacheInfo>>();

            _HohoemaApp.OnSignin += _HohoemaApp_OnSignin;
        }

        private void VideoDownloadManager_DownloadStarted(object sender, NicoVideoCacheProgress request)
        {
            VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
            {
                Request = request,
                CacheState = NicoVideoCacheState.Downloading
            } );
        }

        private async void VideoDownloadManager_DownloadCompleted(object sender, NicoVideoCacheInfo cacheInfo)
        {
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                _CacheVideos.AddOrUpdate(cacheInfo.RawVideoId, 
                    (x) =>
                    {
                        return new List<NicoVideoCacheInfo>() { cacheInfo };
                    }, 
                    (x, y) =>
                    {
                        var info = y.FirstOrDefault(z => z.Quality == cacheInfo.Quality);
                        if (info == null)
                        {
                            y.Add(cacheInfo);
                        }
                        else
                        {
                            info.RequestAt = cacheInfo.RequestAt;
                            info.FilePath = cacheInfo.FilePath;
                        }
                        return y;
                    });
            }

            VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
            {
                Request = cacheInfo,
                CacheState = NicoVideoCacheState.Cached
            });
        }

        private void VideoDownloadManager_DownloadCanceled(object sender, NicoVideoCacheRequest request)
        {
            VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
            {
                Request = request,
                CacheState = NicoVideoCacheState.Pending
            });
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
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    var quality = NicoVideoQualityFileNameHelper.NicoVideoQualityFromFileNameExtention(file.Name);
                    var info = new NicoVideoCacheInfo()
                    {
                        RawVideoId = id,
                        Quality = quality,
                        FilePath = file.Path,
                        RequestAt = file.DateCreated.Date
                    };


                    using (var releaser = await _CacheVideosLock.LockAsync())
                    {
                        _CacheVideos.AddOrUpdate(info.RawVideoId,
                        (x) =>
                        {
                            return new List<NicoVideoCacheInfo>() { info };
                        },
                        (x, y) =>
                        {
                            var tempinfo = y.FirstOrDefault(z => z.Quality == info.Quality);
                            if (tempinfo == null)
                            {
                                y.Add(info);
                            }
                            else
                            {
                                tempinfo.RequestAt = info.RequestAt;
                                tempinfo.FilePath = info.FilePath;
                            }
                            return y;
                        });
                    }

                    VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                    {
                        Request = info,
                        CacheState = NicoVideoCacheState.Cached
                    });

                    Debug.Write(".");
                }
			}
		}

        internal async Task OnCacheFolderChanged()
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


        public Task CacheRequest(string videoId, NicoVideoQuality quality)
        {
            return CacheRequest(new NicoVideoCacheRequest()
            {
                RawVideoId = videoId,
                Quality = quality
            });
        }

        public async Task CacheRequest(NicoVideoCacheRequest req)
        {
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                await VideoDownloadManager.AddCacheRequest(req);
            }
        }

        public async Task CacheRequestCancel(string videoId)
        {
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                await VideoDownloadManager.RemoveCacheRequest(videoId);
            }
        }

        public async Task CacheRequestCancel(string videoId, NicoVideoQuality quality)
        {
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                await VideoDownloadManager.RemoveCacheRequest(videoId, quality);
            }
        }

        public async Task<bool> DeleteCachedVideo(string videoId, NicoVideoQuality quality)
        {
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                if (_CacheVideos.TryGetValue(videoId, out var cachedItems))
                {
                    var removeCached = cachedItems.FirstOrDefault(x => x.RawVideoId == videoId && x.Quality == quality);
                    if (removeCached != null)
                    {
                        var result = await removeCached.Delete();
                        if (result)
                        {
                            cachedItems.Remove(removeCached);
                        }

                        if (cachedItems.Count == 0)
                        {
                            _CacheVideos.TryRemove(videoId, out var list);
                        }

                        return result;
                    }
                }

                return false;
            }
        }

        public async Task<int> DeleteCachedVideo(string videoId)
        {
            int deletedCount = 0;
            using (var releaser = await _CacheVideosLock.LockAsync())
            {
                if (_CacheVideos.TryRemove(videoId, out var cachedItems))
                {
                    foreach (var target in cachedItems)
                    {
                        if (await DeleteCachedVideo(videoId, target.Quality))
                        {
                            deletedCount++;
                        }
                    }
                }
            }

            return deletedCount;
        }


        internal async Task VideoDeletedFromNiconicoServer(string videoId)
        {
            // キャッシュ登録を削除
            int deletedCount = 0;
            try
            {
                deletedCount = await DeleteCachedVideo(videoId);
            }
            catch
            {
                // 削除に失敗
            }

            if (deletedCount > 0)
            {
                var videoInfo = Database.NicoVideoDb.Get(videoId);
                var toastService = App.Current.Container.Resolve<Views.Service.ToastNotificationService>();
                toastService.ShowText("動画削除：" + videoId
                    , $"『{videoInfo?.Title ?? videoId}』 はニコニコ動画サーバーから削除されたため、キャッシュを強制削除しました。"
                    , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                    );

            }
        }

        public NicoVideoCacheInfo GetCacheInfo(string videoId, NicoVideoQuality quality)
        {
            if (_CacheVideos.TryGetValue(videoId, out var list))
            {
                return list.FirstOrDefault(x => x.Quality == quality);
            }
            else
            {
                return null;
            }
        }

    }




    public class NicoVideoCacheProgress : NicoVideoCacheRequest
    {
        public DownloadOperation DownloadOperation { get; set; }

        public NicoVideoCacheProgress()
        {

        }

        public NicoVideoCacheProgress(NicoVideoCacheRequest req, DownloadOperation op)
        {
            RawVideoId = req.RawVideoId;
            Quality = req.Quality;
            IsRequireForceUpdate = req.IsRequireForceUpdate;
            RequestAt = req.RequestAt;
            DownloadOperation = op;
        }
    }







}

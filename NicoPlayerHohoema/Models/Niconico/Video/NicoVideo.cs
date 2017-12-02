using Mntone.Nico2;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
using System.Collections.Immutable;

namespace NicoPlayerHohoema.Models
{


    public class NicoVideo
	{
        public CommentClient CommentClient { get; private set; }

        public string RawVideoId { get; private set; }


        public bool IsForceSmileLowQuality { get; set; }

        NiconicoContentProvider _ContentProvider;
        VideoCacheManager CacheManager;
        NiconicoContext _Context;


        private object _LastAccessResponse;
        public WatchApiResponse LastAccessWatchApiResponse => _LastAccessResponse as WatchApiResponse;
        public DmcWatchData LastAccessDmcWatchResponse => _LastAccessResponse as DmcWatchData;

        public NicoVideo(string rawVideoid, NiconicoContentProvider contentProvider, NiconicoContext context, VideoCacheManager manager)
		{
            RawVideoId = rawVideoid;
            _ContentProvider = contentProvider;
            _Context = context;
			CacheManager = manager;

            CommentClient = new CommentClient(_Context, RawVideoId);            
        }

        #region Playback


        /// <summary>
		/// 動画ストリームの取得します
		/// 他にダウンロードされているアイテムは強制的に一時停止し、再生終了後に再開されます
		/// </summary>
        /// <exception cref="NotSupportedException" />
		public async Task<IVideoStreamingSession> CreateVideoStreamingSession(NicoVideoQuality quality, bool forceDownload = false)
        {
            // オンラインの場合は削除済みかを確認する
            object watchRes = null;

            if (Helpers.InternetConnection.IsInternet())
            {
                // 動画視聴ページへアクセス
                // 動画再生準備およびコメント取得準備が行われる
                watchRes = await VisitWatchPage(quality);

                // 動画情報ページアクセスだけでも内部の動画情報データは更新される
                var videoInfo = Database.NicoVideoDb.Get(RawVideoId);

                // 動作互換性のためにサムネから拾うように戻す？
                // var videoInfo = await _ContentProvider.GetNicoVideoInfo(RawVideoId);

                if (videoInfo.IsDeleted)
                {
                    // ニコニコサーバー側で動画削除済みの場合は再生不可
                    // （NiconnicoContentProvider側で動画削除動作を実施している）
                    throw new NotSupportedException($"動画は {videoInfo.PrivateReasonType.ToCulturelizeString()} のため視聴できません");
                }

            }

            if (!forceDownload)
            {
                // キャッシュ済みアイテムを問い合わせ
                var cacheRequests = await CacheManager.GetCacheRequest(RawVideoId);

                NicoVideoCacheRequest playCandidateRequest = null;
                var req = cacheRequests.FirstOrDefault(x => x.Quality == quality);

                if (req is NicoVideoCacheInfo || req is NicoVideoCacheProgress)
                {
                    playCandidateRequest = req;
                }

                if (req == null)
                {
                    var playableReq = cacheRequests.Where(x => x is NicoVideoCacheInfo || x is NicoVideoCacheProgress);
                    if (playableReq.Any())
                    {
                        // 画質指定がない、または指定画質のキャッシュがない場合には
                        // キャッシュが存在する画質（高画質優先）を取り出す
                        playCandidateRequest = playableReq.OrderBy(x => x.Quality).FirstOrDefault();
                    }
                }

                if (playCandidateRequest is NicoVideoCacheInfo)
                {
                    var playCandidateCache = playCandidateRequest as NicoVideoCacheInfo;
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(playCandidateCache.FilePath);
                        return new LocalVideoStreamingSession(file, playCandidateCache.Quality, _Context);
                    }
                    catch
                    {
                        Debug.WriteLine("動画視聴時にキャッシュが見つかったが、キャッシュファイルを利用した再生セッションの作成に失敗、オンライン再生を試行します");
                    }
                }
                else if (playCandidateRequest is NicoVideoCacheProgress)
                {
                    /*
                    if (Helpers.ApiContractHelper.IsFallCreatorsUpdateAvailable)
                    {
                        var playCandidateCacheProgress = playCandidateRequest as NicoVideoCacheProgress;
                        var op = playCandidateCacheProgress.DownloadOperation;
                        var refStream = op.GetResultRandomAccessStreamReference();
                        return new DownloadProgressVideoStreamingSession(refStream, playCandidateCacheProgress.Quality);
                    }
                    */
                }
            }

            // キャッシュがない場合はオンラインで再生
            if (watchRes is WatchApiResponse)
            {
                var res = watchRes as WatchApiResponse;
                if (res.flashvars.movie_type == "swf")
                {
                    throw new NotSupportedException("SWF形式の動画はサポートしていません");
                }

                if (res.VideoUrl.OriginalString.StartsWith("rtmp"))
                {
                    throw new NotSupportedException("RTMP形式の動画はサポートしていません");
                }

                if (res.IsDeleted)
                {
                    throw new NotSupportedException("動画は削除されています");
                }

                return new SmileVideoStreamingSession(
                    res.VideoUrl,
                    _Context
                    );
            }
            else if (watchRes is DmcWatchData)
            {
                var res = watchRes as DmcWatchData;

                if (res.DmcWatchResponse.Video.IsDeleted)
                {
                    throw new NotSupportedException("動画は削除されています");
                }

                if (res.DmcWatchResponse.Video.DmcInfo != null)
                {
                    if (res.DmcWatchResponse.Video?.DmcInfo?.Quality == null)
                    {
                        throw new NotSupportedException("動画の視聴権がありません");
                    }

                    if (quality.IsLegacy() && res.DmcWatchResponse.Video.SmileInfo != null)
                    {
                        return new SmileVideoStreamingSession(
                            new Uri(res.DmcWatchResponse.Video.SmileInfo.Url),
                            _Context
                            );
                    }
                    else
                    {
                        return new DmcVideoStreamingSession(
                            res,
                            quality.IsDmc() ? quality : NicoVideoQuality.Dmc_High,
                            _Context
                            );
                    }
                }
                else if (res.DmcWatchResponse.Video.SmileInfo != null)
                {
                    return new SmileVideoStreamingSession(
                        new Uri(res.DmcWatchResponse.Video.SmileInfo.Url),
                        _Context
                        );
                }
                else
                {
                    throw new NotSupportedException("動画ページ情報から動画ファイルURLを検出できませんでした");
                }
            }
            else
            {
                throw new NotSupportedException("動画の再生準備に失敗（動画ページの解析でエラーが発生）");
            }
        }

        #endregion

        private async Task<DmcWatchData> GetDmcWatchResponse()
        {
            return await _ContentProvider.GetDmcWatchResponse(RawVideoId);
        }

        private async Task<WatchApiResponse> GetWatchApiResponse(bool forceLoqQuality = false)
        {
            return await _ContentProvider.GetWatchApiResponse(RawVideoId, forceLoqQuality);
        }


        public async Task<object> VisitWatchPage(NicoVideoQuality quality)
		{
            if (!Helpers.InternetConnection.IsInternet()) { return null; }
            
            object res = null;
            try
            {
                if (quality.IsLegacy())
                {
                    res = await GetWatchApiResponse(IsForceSmileLowQuality);
                }
                else
                {
                    var dmcRes = await GetDmcWatchResponse();
                    res = dmcRes;
                }
            }
            catch
            {
                if (quality.IsLegacy()) { throw; }
                await Task.Delay(TimeSpan.FromSeconds(1));
                res = await GetWatchApiResponse(IsForceSmileLowQuality);
            }

            _LastAccessResponse = res;

            if (res is WatchApiResponse)
            {
                var watchApiRes = res as WatchApiResponse;
                CommentClient = new CommentClient(_Context, new CommentServerInfo()
                {
                    ServerUrl = watchApiRes.CommentServerUrl.OriginalString,
                    VideoId = RawVideoId,
                    DefaultThreadId = (int)watchApiRes.ThreadId,
                    CommunityThreadId = (int)watchApiRes.OptionalThreadId,
                    ViewerUserId = watchApiRes.viewerInfo.id,
                    ThreadKeyRequired = watchApiRes.IsKeyRequired
                });

                return res;
            }
            else if (res is DmcWatchData)
            {
                var watchdata = res as DmcWatchData;
                var dmcRes = watchdata.DmcWatchResponse;
                CommentClient = new CommentClient(_Context, new CommentServerInfo()
                {
                    ServerUrl = dmcRes.Thread.ServerUrl,
                    VideoId = RawVideoId,
                    DefaultThreadId = int.Parse(dmcRes.Thread.Ids.Default),
                    ViewerUserId = dmcRes.Viewer.Id,
                    ThreadKeyRequired = dmcRes.Video.IsOfficial
                })
                {
                    LastAccessDmcWatchResponse = dmcRes
                };

                if (int.TryParse(dmcRes.Thread.Ids.Community, out var communityThreadId))
                {
                    CommentClient.CommentServerInfo.CommunityThreadId = communityThreadId;
                }

                return res;
            }
            else
            {
                return null;
            }
        }
	}
}

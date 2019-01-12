using Mntone.Nico2;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.WatchAPI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Niconico.Video;
using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models.Niconico;
using Unity;
using NicoPlayerHohoema.Models.Provider;
using Prism.Unity;

namespace NicoPlayerHohoema.Models
{


    public class NicoVideoStreamingSessionProvider : INiconicoStreamingSessionProvider, INiconicoCommentSessionProvider
	{
        // 動画情報ページへのアクセスを提供する

        public NicoVideoStreamingSessionProvider()
		{
            NicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            NiconicoSession = App.Current.Container.Resolve<NiconicoSession>();
        }

        public Provider.NicoVideoProvider NicoVideoProvider { get; }
        public NiconicoSession NiconicoSession { get; }

        private object _LastAccessResponse;
        private WatchApiResponse LastAccessWatchApiResponse => _LastAccessResponse as WatchApiResponse;
        private DmcWatchData LastAccessDmcWatchResponse => _LastAccessResponse as DmcWatchData;



        public async Task<ICommentSession> CreateCommentSessionAsync(string contentId)
        {
            var res = await VisitWatchPage(contentId);

            var commentClient = new CommentClient(NiconicoSession, contentId);
            if (res is WatchApiResponse watchApiRes)
            {
                commentClient.CommentServerInfo = new CommentServerInfo()
                {
                    ServerUrl = watchApiRes.CommentServerUrl.OriginalString,
                    VideoId = contentId,
                    DefaultThreadId = (int)watchApiRes.ThreadId,
                    CommunityThreadId = (int)watchApiRes.OptionalThreadId,
                    ViewerUserId = watchApiRes.viewerInfo.id,
                    ThreadKeyRequired = watchApiRes.IsKeyRequired
                };
                commentClient.VideoOwnerId = watchApiRes.UploaderInfo?.id;
            }
            else if (res is DmcWatchData watchData)
            {
                var dmcRes = watchData.DmcWatchResponse;
                commentClient.CommentServerInfo = new CommentServerInfo()
                {
                    ServerUrl = dmcRes.Thread.ServerUrl,
                    VideoId = contentId,
                    DefaultThreadId = int.Parse(dmcRes.Thread.Ids.Default),
                    ViewerUserId = dmcRes.Viewer.Id,
                    ThreadKeyRequired = dmcRes.Video.IsOfficial
                };
                
                // チャンネル動画ではOnwerはnullになる
                commentClient.VideoOwnerId = dmcRes.Owner?.Id;

                commentClient.DmcWatch = dmcRes;

                if (int.TryParse(dmcRes.Thread.Ids.Community, out var communityThreadId))
                {
                    commentClient.CommentServerInfo.CommunityThreadId = communityThreadId;
                    Debug.WriteLine("dmcRes.Video.DmcInfo.Thread.PostkeyAvailable: " + dmcRes.Video.DmcInfo?.Thread?.PostkeyAvailable);
                }
            }

            
            return new VideoCommentService(commentClient);
        }


        #region Playback


        /// <summary>
        /// 動画ストリームの取得します
        /// 他にダウンロードされているアイテムは強制的に一時停止し、再生終了後に再開されます
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public async Task<IStreamingSession> CreateStreamingSessionAsync(string contentId, NicoVideoQuality requestedQuality = NicoVideoQuality.Unknown)
        {
            // オンラインの場合は削除済みかを確認する
            object watchRes = null;

            if (Helpers.InternetConnection.IsInternet())
            {
                // 動画視聴ページへアクセス
                // 動画再生準備およびコメント取得準備が行われる
                watchRes = await VisitWatchPage(contentId);

                // 動画情報ページアクセスだけでも内部の動画情報データは更新される
                var videoInfo = Database.NicoVideoDb.Get(contentId);

                if (videoInfo.IsDeleted)
                {
                    // ニコニコサーバー側で動画削除済みの場合は再生不可
                    // （NicoVideoProvider側で動画削除動作を実施している）
                    throw new NotSupportedException($"動画は {videoInfo.PrivateReasonType.ToCulturelizeString()} のため視聴できません");
                }

            }
                
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
                    NiconicoSession
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

                    return new DmcVideoStreamingSession(
                            res,
                            requestedQuality,
                            NiconicoSession
                            );
                }
                else if (res.DmcWatchResponse.Video.SmileInfo != null)
                {
                    return new SmileVideoStreamingSession(
                        new Uri(res.DmcWatchResponse.Video.SmileInfo.Url),
                        NiconicoSession
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

        public async Task<object> VisitWatchPage(string rawVideoId)
		{
            if (!Helpers.InternetConnection.IsInternet()) { return null; }
            
            object res = null;
            try
            {
                var dmcRes = await NicoVideoProvider.GetDmcWatchResponse(rawVideoId);
                res = dmcRes;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                res = await NicoVideoProvider.GetWatchApiResponse(rawVideoId);
            }

            _LastAccessResponse = res;

            return res;
        }



        public async Task<List<Database.NicoVideo>> GetRelatedVideos(string rawVideoid)
        {
            if (LastAccessDmcWatchResponse?.DmcWatchResponse.Playlist != null)
            {
                // TODO: 動画プレイリスト情報の取得をProvider.NicoVideoProviderへ移す
                var res = await NiconicoSession.Context.Video.GetVideoPlaylistAsync(rawVideoid, LastAccessDmcWatchResponse?.DmcWatchResponse.Playlist.Referer);

                if (res.Status == "ok")
                {
                    return res.Data.Items
                        .Select(x =>
                        {
                            var videoData = Database.NicoVideoDb.Get(x.Id);
                            videoData.Title = x.Title;
                            videoData.Length = TimeSpan.FromSeconds(x.LengthSeconds);
                            videoData.PostedAt = DateTime.Parse(x.FirstRetrieve);
                            videoData.ThumbnailUrl = x.ThumbnailURL;
                            videoData.ViewCount = x.ViewCounter;
                            videoData.MylistCount = x.MylistCounter;
                            videoData.CommentCount = x.NumRes ?? 0;

                            Database.NicoVideoDb.AddOrUpdate(videoData);

                            return videoData;
                        })
                        .ToList();
                }
            }

            return new List<Database.NicoVideo>();
        }


        static readonly Regex NiconicoContentUrlRegex = new Regex(@"https?:\/\/[a-z]+\.nicovideo\.jp\/([a-z]+)\/([a-z][a-z][0-9]+|[0-9]+)");

        static readonly Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");

        public static VideoRelatedInfomation GetVideoRelatedInfomationWithVideoDescription(string rawVideoId)
        {
            var nicoVideo = Database.NicoVideoDb.Get(rawVideoId);

            if (string.IsNullOrEmpty(nicoVideo.DescriptionWithHtml)) { return null; }

            VideoRelatedInfomation info = new VideoRelatedInfomation();
            var niconicoContentMatchs = NiconicoContentUrlRegex.Matches(nicoVideo.DescriptionWithHtml);
            foreach (var match in niconicoContentMatchs.Cast<Match>())
            {
                var contentType = match.Groups[1].Value;
                var contentId = match.Groups[2].Value;

                // TODO: 
                info.NiconicoContentIds.Add(new NiconicoContent()
                {
                    Type = contentType,
                    Id = contentId
                });
            }

            return info;
        }

        public static IList<Uri> GetGeneralUrlsWithVideoDescription(string rawVideoId)
        {
            var nicoVideo = Database.NicoVideoDb.Get(rawVideoId);

            if (string.IsNullOrEmpty(nicoVideo.DescriptionWithHtml)) { return null; }

            List<Uri> uris = new List<Uri>();
            var generalUrlMatchs = GeneralUrlRegex.Matches(nicoVideo.DescriptionWithHtml);

            foreach (var match in generalUrlMatchs.Cast<Match>().Where(x => !NiconicoContentUrlRegex.IsMatch(x.Value)))
            {
                var url = match.Groups[1].Value;
                uris.Add(new Uri(url));
            }

            return uris;
        }


    }


    // 動画情報
    public class VideoRelatedInfomation
    {
        public IList<NiconicoContent> NiconicoContentIds { get; } = new List<NiconicoContent>();

        public IEnumerable<string> GetVideoIds()
        {
            return NiconicoContentIds.Where(x => x.Type == "watch" &&
                (x.Id.StartsWith("sm") || x.Id.StartsWith("so") || x.Id.StartsWith("nm"))
                )
                .Select(x => x.Id);
        }

        public IEnumerable<string> GetMylistIds()
        {
            return NiconicoContentIds.Where(x => x.Type == "mylist")
                .Select(x => x.Id);
        }
    }

    public class NiconicoContent
    {
        public string Type { get; set; }
        public string Id { get; set; }
    }
}

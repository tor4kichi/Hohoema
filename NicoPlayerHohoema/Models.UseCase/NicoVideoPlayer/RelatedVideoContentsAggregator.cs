using Microsoft.Toolkit.Uwp.UI;
using Mntone.Nico2.Channels.Video;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Presentation.Services.Page;

namespace Hohoema.Models.UseCase.NicoVideoPlayer
{
    public class VideoRelatedContents
    {
        public VideoRelatedContents(string contentId)
        {
            ContentId = contentId;
        }
        public VideoInfoControlViewModel CurrentVideo { get; internal set; }

        public VideoInfoControlViewModel NextVideo { get; internal set; }

        public List<VideoInfoControlViewModel> Videos { get; internal set; } = new List<VideoInfoControlViewModel>();
        public List<VideoInfoControlViewModel> OtherVideos { get; internal set; } = new List<VideoInfoControlViewModel>();

        public List<MylistPlaylist> Mylists { get; internal set; } = new List<MylistPlaylist>();
        public string ContentId { get; }
    }

    public sealed class RelatedVideoContentsAggregator 
    {
        public RelatedVideoContentsAggregator(
           NicoVideoProvider nicoVideoProvider,
           ChannelProvider channelProvider,
           MylistRepository mylistRepository,
           HohoemaPlaylist hohoemaPlaylist,
           PageManager pageManager,
           NicoVideoCacheRepository nicoVideoRepository,
           NicoChannelCacheRepository nicoChannelCacheRepository
           )
        {
            NicoVideoProvider = nicoVideoProvider;
            ChannelProvider = channelProvider;
            _mylistRepository = mylistRepository;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            _nicoVideoRepository = nicoVideoRepository;
            _nicoChannelCacheRepository = nicoChannelCacheRepository;
        }

        public NicoVideoSessionProvider Video { get; }

        public string JumpVideoId { get; set; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public bool HasVideoDescription { get; private set; }

        const double _SeriesVideosTitleSimilarityValue = 0.7;
        private readonly MylistRepository _mylistRepository;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NicoChannelCacheRepository _nicoChannelCacheRepository;
        private static VideoRelatedContents _cachedVideoRelatedContents;

        public async Task<VideoRelatedContents> GetRelatedContentsAsync(string videoId)
        {
            if (_cachedVideoRelatedContents?.ContentId == videoId)
            {
                return _cachedVideoRelatedContents;
            }

            var videoInfo = _nicoVideoRepository.Get(videoId);
            var videoViewerHelpInfo = NicoVideoSessionProvider.GetVideoRelatedInfomationWithVideoDescription(videoId, videoInfo.DescriptionWithHtml);

            VideoRelatedContents result = new VideoRelatedContents(videoId);
            // ニコスクリプトで指定されたジャンプ先動画
            /*
            if (JumpVideoId != null)
            {
                var video = await NicoVideoProvider.GetNicoVideoInfo(JumpVideoId, requireLatest: true);
                if (video != null)
                {
                    JumpVideo = new VideoInfoControlViewModel(video);
                    RaisePropertyChanged(nameof(JumpVideo));
                }
            }
            */

            // 再生中アイテムのタイトルと投稿者説明文に含まれる動画IDの動画タイトルを比較して
            // タイトル文字列が近似する動画をシリーズ動画として取り込む
            // 違うっぽい動画も投稿者が提示したい動画として確保
            var videoIds = videoViewerHelpInfo.GetVideoIds();
            List<NicoVideo> seriesVideos = new List<NicoVideo>();
            seriesVideos.Add(videoInfo);
            foreach (var id in videoIds)
            {
                var video = await NicoVideoProvider.GetNicoVideoInfo(id, requireLatest: true);

                var titleSimilarity = videoInfo.Title.CalculateSimilarity(video.Title);
                if (titleSimilarity > _SeriesVideosTitleSimilarityValue)
                {
                    seriesVideos.Add(video);
                }
                else
                {
                    var otherVideo = new VideoInfoControlViewModel(video);
                    result.OtherVideos.Add(otherVideo);
                }
            }


            // シリーズ動画として集めたアイテムを投稿日が新しいものが最後尾になるよう並び替え
            // シリーズ動画に番兵として仕込んだ現在再生中のアイテムの位置と動画数を比べて
            // 動画数が上回っていた場合は次動画が最後尾にあると決め打ちで取得する
            var orderedSeriesVideos = seriesVideos.OrderBy(x => x.PostedAt).ToList();
            var currentVideoIndex = orderedSeriesVideos.IndexOf(videoInfo);
            if (orderedSeriesVideos.Count - 1 > currentVideoIndex)
            {
                var nextVideo = orderedSeriesVideos.Last();
                if (nextVideo.RawVideoId != videoId)
                {
                    result.NextVideo = new VideoInfoControlViewModel(nextVideo);

                    orderedSeriesVideos.Remove(nextVideo);
                }
            }

            // 次動画を除いてシリーズ動画っぽいアイテムを投稿者が提示したい動画として優先表示されるようにする
            orderedSeriesVideos.Remove(videoInfo);
            orderedSeriesVideos.Reverse();
            foreach (var video in orderedSeriesVideos)
            {
                var videoVM = new VideoInfoControlViewModel(video);
                result.OtherVideos.Insert(0, videoVM);
            }


            // チャンネル動画で次動画が見つからなかった場合は
            // チャンネル動画一覧から次動画をサジェストする
            if (videoInfo.Owner.UserType == NicoVideoUserType.Channel
                && result.NextVideo == null
                )
            {
                // DBからチャンネル情報を取得
                var db_channelInfo = _nicoChannelCacheRepository.GetFromRawId(videoInfo.Owner.OwnerId);
                if (db_channelInfo == null)
                {
                    db_channelInfo = new NicoChannelInfo()
                    {
                        RawId = videoInfo.Owner.OwnerId,
                        ThumbnailUrl = videoInfo.Owner.IconUrl,
                        Name = videoInfo.Owner.ScreenName
                    };
                }

                // チャンネル動画の一覧を取得する
                // ページアクセスが必要なので先頭ページを取って
                // 全体の分量を把握してから全ページ取得を行う
                List<ChannelVideoInfo> channelVideos = new List<ChannelVideoInfo>();
                var channelVideosFirstPage = await ChannelProvider.GetChannelVideo(videoInfo.Owner.OwnerId, 0);
                var uncheckedCount = channelVideosFirstPage.TotalCount - channelVideosFirstPage.Videos.Count;
                if (channelVideosFirstPage.TotalCount != 0)
                {
                    channelVideos.AddRange(channelVideosFirstPage.Videos);

                    var uncheckedPageCount = (int)Math.Ceiling((double)uncheckedCount / 20); /* チャンネル動画１ページ = 20 動画 */
                    foreach (var page in Enumerable.Range(1, uncheckedPageCount))
                    {
                        var channelVideoInfo = await ChannelProvider.GetChannelVideo(videoInfo.Owner.OwnerId, page);
                        channelVideos.AddRange(channelVideoInfo.Videos);
                    }

                    db_channelInfo.Videos = channelVideos;
                }

                _nicoChannelCacheRepository.AddOrUpdate(db_channelInfo);


                var collectionView = new AdvancedCollectionView(db_channelInfo.Videos);
                collectionView.SortDescriptions.Add(new SortDescription(nameof(ChannelVideoInfo.PostedAt), SortDirection.Ascending));
                collectionView.SortDescriptions.Add(new SortDescription(nameof(ChannelVideoInfo.ItemId), SortDirection.Ascending));
                collectionView.RefreshSorting();

                var item = collectionView.FirstOrDefault(x => (x as ChannelVideoInfo).Title == videoInfo.Title);
                var pos = collectionView.IndexOf(item);
                if (pos >= 0)
                {
                    var nextVideo = collectionView.ElementAtOrDefault(pos + 1) as ChannelVideoInfo;
                    if (nextVideo != null)
                    {
                        var videoVM = new VideoInfoControlViewModel(nextVideo.ItemId);
                        videoVM.IsRequirePayment = nextVideo.IsRequirePayment;
                        videoVM.SetTitle(nextVideo.Title);
                        videoVM.SetSubmitDate(nextVideo.PostedAt);
                        videoVM.SetThumbnailImage(nextVideo.ThumbnailUrl);
                        videoVM.SetVideoDuration(nextVideo.Length);
                        videoVM.SetDescription(nextVideo.ViewCount, nextVideo.CommentCount, nextVideo.MylistCount);

                        result.NextVideo = videoVM;
                    }
                }
            }

            // マイリスト
            var relatedMylistIds = videoViewerHelpInfo.GetMylistIds();
            foreach (var mylistId in relatedMylistIds)
            {
                var mylist = await _mylistRepository.GetMylist(mylistId);
                if (mylist != null)
                {
                    result.Mylists.Add(mylist);
                }
            }


            /*
            var videos = await Video.GetRelatedVideos(videoId);
            Videos = videos.Select(x =>
            {
                var vm = new VideoInfoControlViewModel(x);
                return vm;
            })
            .ToList();
            */

            result.CurrentVideo = result.Videos.FirstOrDefault(x => x.RawVideoId == videoId);

            _cachedVideoRelatedContents = result;

            return result;
        }
    }
}

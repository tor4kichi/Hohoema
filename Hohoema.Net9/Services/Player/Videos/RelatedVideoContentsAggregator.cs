﻿#nullable enable
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player.Video;
using Hohoema.Services.Playlist;
using Hohoema.ViewModels.VideoListPage;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Services.Player;

public class VideoRelatedContents
{
    public VideoRelatedContents(string contentId)
    {
        ContentId = contentId;
    }
    public VideoListItemControlViewModel CurrentVideo { get; internal set; }

    public VideoListItemControlViewModel NextVideo { get; internal set; }

    public List<VideoListItemControlViewModel> Videos { get; internal set; } = new List<VideoListItemControlViewModel>();
    public List<VideoListItemControlViewModel> OtherVideos { get; internal set; } = new List<VideoListItemControlViewModel>();

    public List<MylistPlaylist> Mylists { get; internal set; } = new List<MylistPlaylist>();
    public VideoId ContentId { get; }
}

public sealed class RelatedVideoContentsAggregator 
{
    public RelatedVideoContentsAggregator(
        NiconicoSession niconicoSession,
       NicoVideoProvider nicoVideoProvider,
       ChannelProvider channelProvider,
       MylistResolver mylistRepository
       )
    {
        _niconicoSession = niconicoSession;
        _nicoVideoProvider = nicoVideoProvider;
        _channelProvider = channelProvider;
        _mylistRepository = mylistRepository;       
    }

    private readonly NiconicoSession _niconicoSession;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly ChannelProvider _channelProvider;
    private readonly MylistResolver _mylistRepository;

    public NicoVideoSessionProvider Video { get; }

    public string JumpVideoId { get; set; }
    public bool HasVideoDescription { get; private set; }

    const double _SeriesVideosTitleSimilarityValue = 0.7;
    private static VideoRelatedContents _cachedVideoRelatedContents;

    public async Task<VideoRelatedContents> GetRelatedContentsAsync(INicoVideoDetails currentVideo)
    {
        var videoId = currentVideo.VideoId;
        if (_cachedVideoRelatedContents?.ContentId == videoId)
        {
            return _cachedVideoRelatedContents;
        }

        var videoInfo = _nicoVideoProvider.GetCachedVideoInfo(videoId);
        var videoViewerHelpInfo = NicoVideoSessionProvider.GetVideoRelatedInfomationWithVideoDescription(videoId, videoInfo.Description);

        VideoRelatedContents result = new VideoRelatedContents(videoId);            

        // 再生中アイテムのタイトルと投稿者説明文に含まれる動画IDの動画タイトルを比較して
        // タイトル文字列が近似する動画をシリーズ動画として取り込む
        // 違うっぽい動画も投稿者が提示したい動画として確保
        var videoIds = videoViewerHelpInfo.GetVideoIds();
        List<NicoVideo> seriesVideos = new List<NicoVideo>();
        seriesVideos.Add(videoInfo);
        foreach (var id in videoIds)
        {
            var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(id);

            var titleSimilarity = videoInfo.Title.CalculateSimilarity(video.Title);
            if (titleSimilarity > _SeriesVideosTitleSimilarityValue)
            {
                seriesVideos.Add(video);
            }
            else
            {
                var otherVideo = new VideoListItemControlViewModel(video);
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
            if (nextVideo.Id != videoId)
            {
                result.NextVideo = new VideoListItemControlViewModel(nextVideo);

                orderedSeriesVideos.Remove(nextVideo);
            }
        }

        // 次動画を除いてシリーズ動画っぽいアイテムを投稿者が提示したい動画として優先表示されるようにする
        orderedSeriesVideos.Remove(videoInfo);
        orderedSeriesVideos.Reverse();
        foreach (var video in orderedSeriesVideos)
        {
            var videoVM = new VideoListItemControlViewModel(video);
            result.OtherVideos.Insert(0, videoVM);
        }


        // マイリスト
        var relatedMylistIds = videoViewerHelpInfo.GetMylistIds();
        foreach (var mylistId in relatedMylistIds)
        {
            var mylist = await _mylistRepository.GetMylistAsync(mylistId);
            if (mylist != null)
            {
                result.Mylists.Add(mylist);
            }
        }


        VideoRecommendResponse recommendResponse = null;
        if (currentVideo is IVideoContentProvider provider)
        {
            if (provider.ProviderType == OwnerType.Channel)
            {
                recommendResponse = await _niconicoSession.ToolkitContext.Recommend.GetVideoRecommendForChannelAsync(currentVideo.VideoId, provider.ProviderId, currentVideo.Tags.Select(x => x.Tag).ToArray());
            }
        }

        if (recommendResponse == null)
        {
            recommendResponse = await _niconicoSession.ToolkitContext.Recommend.GetVideoRecommendForNotChannelAsync(currentVideo.VideoId);
        }

        if (recommendResponse?.IsSuccess ?? false)
        {
            result.OtherVideos = new List<VideoListItemControlViewModel>();
            foreach (var item in recommendResponse.Data.Items)
            {
                if (item.ContentType is RecommendContentType.Video)
                {
                    result.OtherVideos.Add(new VideoListItemControlViewModel(item.ContentAsVideo));
                }
            }
        }

        result.CurrentVideo = result.Videos.FirstOrDefault(x => x.VideoId == videoId);

        _cachedVideoRelatedContents = result;

        return result;
    }
}

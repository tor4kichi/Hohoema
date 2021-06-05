using AngleSharp.Html.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace NiconicoToolkit.Series
{
    public sealed class SeriesClient
    {
        private readonly NiconicoContext _context;

        public SeriesClient(NiconicoContext context)
        {
            _context = context;
        }

        public async Task<SeriesDetails> GetSeriesVideosAsync(string seriesId)
        {
            HtmlParser parser = new HtmlParser();

            var res = await _context.GetAsync($"https://www.nicovideo.jp/series/{seriesId}");
            if (!res.IsSuccessStatusCode)
            {
                throw new WebException($"not found series({seriesId}), status code : {res.StatusCode}");
            }

            SeriesDetails seriesDetails = new SeriesDetails();
            using (var stream = await res.Content.ReadAsInputStreamAsync())
            using (var document = await parser.ParseDocumentAsync(stream.AsStreamForRead()))
            {
                // シリーズの概要
                {
                    var summryParentNode = document.QuerySelector("body > div.BaseLayout-main > div.Series > div.BaseLayout-block.Series-2column > div.Series-columnMain > div > div.SeriesDetailContainer-media");
                    var thumbNode = summryParentNode.QuerySelector("div.Thumbnail-image");
                    var titleNode = summryParentNode.QuerySelector("div.SeriesDetailContainer-body > div.SeriesDetailContainer-bodyTitle");
                    var countNode = summryParentNode.QuerySelector("div.SeriesDetailContainer-body > div.SeriesDetailContainer-bodyMeta");

                    seriesDetails.Series = new Series()
                    {
                        ThumbnailUrl = thumbNode.GetAttribute("data-background-image").ToUri(),
                        Title = titleNode.TextContent,
                        Count = countNode.TextContent.Where(x => char.IsDigit(x)).ToDigit()
                    };
                }

                // 動画一覧
                {
                    var videoListContainerNode = document.QuerySelector("body > div.BaseLayout-main > div.Series > div.BaseLayout-block.Series-2column > div.Series-columnMain > div > div.SeriesVideoListContainer");
                    if (videoListContainerNode == null)
                    {
                        throw new WebException("series not contain video list");
                    }

                    List<SeriesVideo> videos = new List<SeriesVideo>(); ;
                    foreach (var videoNode in videoListContainerNode.ChildNodes.Where(x => x is IHtmlDivElement).Cast<IHtmlDivElement>())
                    {
                        var videoId = videoNode.GetAttribute("data-video-itemdata-video-id");
                        var thumbNode = videoNode.QuerySelector("div.MediaObject-content > div.MediaObject-image > a > div > div.Thumbnail-image");
                        var videoLengthNode = videoNode.QuerySelector("div.MediaObject-content > div.MediaObject-image > a > div > div.VideoLength");

                        var bodyNode = videoNode.QuerySelector("div.MediaObject-content > div.MediaObject-body");
                        var registeredAtNode = bodyNode.QuerySelector("div.SeriesVideoListContainer-videoRegisteredAt");
                        var titleNode = bodyNode.QuerySelector("div.VideoMediaObject-title > a");
                        var descNode = bodyNode.QuerySelector("div.VideoMediaObject-title > div.VideoMediaObject-description");

                        var countMetaNodes = bodyNode.QuerySelectorAll("div.SeriesVideoListContainer-videoMetaCount > span.VideoMetaCount");
                        int? viewCount = null;
                        int? commentCount = null;
                        int? mylistCount = null;
                        foreach (var countNode in countMetaNodes)
                        {
                            if (countNode.ClassList.Contains("VideoMetaCount-view"))
                            {
                                viewCount = countNode.TextContent.ToInt();
                            }
                            else if (countNode.ClassList.Contains("VideoMetaCount-comment"))
                            {
                                commentCount = countNode.TextContent.ToInt();
                            }
                            else if (countNode.ClassList.Contains("VideoMetaCount-mylist"))
                            {
                                mylistCount = countNode.TextContent.ToInt();
                            }
                        }

                        // 投稿日時
                        DateTime postedAt;
                        var videoRegisteredAtText = registeredAtNode.TextContent;
                        if (videoRegisteredAtText.Contains("/"))
                        {
                            // ex) 2020/06/24 18:00 投稿
                            var videoRegisteredAtDateTimeText = string.Join(" ", videoRegisteredAtText.Trim().Split(' ').Take(2));
                            postedAt = videoRegisteredAtDateTimeText.ToDateTimeOffsetFromIso8601().DateTime;
                        }
                        else
                        {
                            if (videoRegisteredAtText.Contains("時間"))
                            {
                                // ex) 19時間前 投稿
                                var time = int.Parse(new string(videoRegisteredAtText.TrimStart(' ', '\t', '\n').TakeWhile(c => char.IsDigit(c)).ToArray()));
                                postedAt = DateTime.Now - TimeSpan.FromHours(time);
                            }
                            else if (videoRegisteredAtText.Contains("分"))
                            {
                                // ex) 19分前 投稿 
                                // があるか知らないけど念の為
                                var time = int.Parse(new string(videoRegisteredAtText.TakeWhile(c => char.IsDigit(c)).ToArray()));
                                postedAt = DateTime.Now - TimeSpan.FromMinutes(time);
                            }
                            else
                            {
                                postedAt = DateTime.Now;
                            }
                        }

                        var seriesVideo = new SeriesVideo()
                        {
                            Id = videoId,
                            ThumbnailUrl = thumbNode.GetAttribute("data-background-image").ToUri(),
                            Title = titleNode.TextContent,
                            Duration = videoLengthNode.TextContent.ToTimeSpan(),
                            PostAt = postedAt,
                            WatchCount = viewCount ?? 0,
                            CommentCount = commentCount ?? 0,
                            MylistCount = mylistCount ?? 0,
                        };

                        videos.Add(seriesVideo);
                    }

                    seriesDetails.Videos = videos;
                }

                // シリーズオーナー
                {
                    var userContainerNode = document.QuerySelector("body > div.BaseLayout-main > div.Series > div.SeriesAdditionalContainer > div.SeriesAdditionalContainer-ownerArea");
                    var iconNode = userContainerNode.QuerySelector("span.UserIcon > img");
                    var userInfoNode = userContainerNode.QuerySelector("a");

                    seriesDetails.Owner = new SeriesOwner()
                    {
                        Id = new string(userInfoNode.GetAttribute("href").SkipWhile(c => !char.IsDigit(c)).ToArray()),
                        IconUrl = iconNode.GetAttribute("src"),
                        Nickname = userInfoNode.TextContent,
                    };
                }
            }

            return seriesDetails;
        }
    }

    public class SeriesDetails
    {
        public Series Series { get; set; }
        public SeriesOwner Owner { get; set; }
        public string DescriptionHTML { get; set; }
        public List<SeriesVideo> Videos { get; set; }
    }

    public class SeriesVideo
    {
        public Uri ThumbnailUrl { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime PostAt { get; set; }
        public int WatchCount { get; set; }
        public int CommentCount { get; set; }
        public int MylistCount { get; set; }
    }




    public class SeriesOwner
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public string IconUrl { get; set; }
    }

    public class SeriesSimple
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }



    public class Series
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int? Count { get; set; }
        public Uri ThumbnailUrl { get; set; }
    }
}

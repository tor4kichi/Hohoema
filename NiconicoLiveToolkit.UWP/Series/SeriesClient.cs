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
using System.Text.Json;
using NiconicoToolkit.Video;
using NiconicoToolkit.User;

namespace NiconicoToolkit.Series
{
    public sealed class SeriesClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        public SeriesClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _options = defaultOptions;
        }

        public async Task<SeriesDetails> GetSeriesVideosAsync(string seriesId)
        {
            HtmlParser parser = new HtmlParser();

            using var res = await _context.GetAsync($"{NiconicoUrls.NicoHomePageUrl}series/{seriesId}");
            if (!res.IsSuccessStatusCode)
            {
                throw new WebException($"not found series({seriesId}), status code : {res.StatusCode}");
            }

            return await res.Content.ReadHtmlDocumentActionAsync(document =>
            {
                SeriesDetails seriesDetails = new SeriesDetails();
                // シリーズの概要
                {
                    var summryParentNode = document.QuerySelector("body > div.BaseLayout-main > div.Series > div.BaseLayout-block.Series-2column > div.Series-columnMain > div > div.SeriesDetailContainer-media");
                    var thumbNode = summryParentNode.QuerySelector("div.Thumbnail-image");
                    var titleNode = summryParentNode.QuerySelector("div.SeriesDetailContainer-body > div.SeriesDetailContainer-bodyTitle");
                    var countNode = summryParentNode.QuerySelector("div.SeriesDetailContainer-body > div.SeriesDetailContainer-bodyMeta");

                    seriesDetails.Series = new SeriesDetails.SeriesItem()
                    {
                        Id = seriesId,
                        ThumbnailUrl = thumbNode.GetAttribute("data-background-image").ToUri(),
                        Title = titleNode.TextContent,
                        Count = countNode.TextContent.Where(x => char.IsDigit(x)).ToInt()
                    };
                }

                // 動画一覧
                {
                    var videoListContainerNode = document.QuerySelector("div.SeriesVideoListContainer");
                    if (videoListContainerNode == null)
                    {
                        throw new WebException("series not contain video list");
                    }

                    List<SeriesDetails.SeriesVideo> videos = new List<SeriesDetails.SeriesVideo>(); ;
                    foreach (var videoNode in videoListContainerNode.ChildNodes.Where(x => x is IHtmlDivElement).Cast<IHtmlDivElement>())
                    {
                        var anchorNode = videoNode.QuerySelector("div > a");
                        var videoId = anchorNode.GetAttribute("href").Split('/').Last();
                        var thumbNode = anchorNode.QuerySelector("div.NC-MediaObject-media > div > div > div > div.NC-Thumbnail-image");
                        var videoLengthNode = anchorNode.QuerySelector("div.NC-MediaObject-media > div > div > div > div.NC-VideoLength");

                        var bodyNode = anchorNode.QuerySelector("div.NC-MediaObject-body");
                        var registeredAtNode = bodyNode.QuerySelector("div.NC-MediaObject-bodySecondary > div.NC-VideoMediaObject-meta > div.NC-VideoMediaObject-metaAdditional > span");
                        var titleNode = bodyNode.QuerySelector("div.NC-MediaObject-bodyTitle > h2");
                        var descNode = bodyNode.QuerySelector("div.NC-MediaObject-bodySecondary > div.NC-VideoMediaObject-description");

                        var countMetaNodes = bodyNode.QuerySelectorAll("div.NC-MediaObject-bodySecondary > div.NC-VideoMediaObject-meta > div.NC-VideoMediaObject-metaCount > div.NC-VideoMetaCount");
                        int? viewCount = null;
                        int? commentCount = null;
                        int? mylistCount = null;
                        int? likeCount = null;
                        foreach (var countNode in countMetaNodes)
                        {
                            static int ReadableNumberToInt(string str)
                            {
                                // 10.6万
                                // 1,544
                                // 183
                                bool isTenThauthand = str.Last() == '万';
                                double number = double.Parse(isTenThauthand ? new string(str.SkipLast(1).ToArray()) : str, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowDecimalPoint);
                                return (int)(isTenThauthand ? number * 10000 : number);
                            }
                            if (countNode.ClassList.Contains("NC-VideoMetaCount_view"))
                            {
                                viewCount = ReadableNumberToInt(countNode.TextContent.Trim());
                            }
                            else if (countNode.ClassList.Contains("NC-VideoMetaCount_comment"))
                            {
                                commentCount = ReadableNumberToInt(countNode.TextContent.Trim());
                            }
                            else if (countNode.ClassList.Contains("NC-VideoMetaCount_mylist"))
                            {
                                mylistCount = ReadableNumberToInt(countNode.TextContent.Trim());
                            }
                            else if (countNode.ClassList.Contains("NC-VideoMetaCount_like"))
                            {
                                likeCount = ReadableNumberToInt(countNode.TextContent.Trim());
                            }
                            //else if (countNode.ClassList.Contains("NC-VideoMetaCount_nicoad"))
                            //{
                            //
                            //}
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
                                var time = int.Parse(new string(videoRegisteredAtText.Trim().TakeWhile(c => char.IsDigit(c)).ToArray()));
                                postedAt = DateTime.Now - TimeSpan.FromMinutes(time);
                            }
                            else
                            {
                                postedAt = DateTime.Now;
                            }
                        }

                        var seriesVideo = new SeriesDetails.SeriesVideo()
                        {
                            Id = videoId,
                            ThumbnailUrl = thumbNode.GetAttribute("data-background-image").ToUri(),
                            Title = titleNode.TextContent.Trim(),
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
                    var iconNode = userContainerNode.QuerySelector("span > img");
                    var userInfoNode = userContainerNode.QuerySelector("a");

                    var id = userInfoNode.GetAttribute("href").Split('/').Last();
                    seriesDetails.Owner = new SeriesDetails.SeriesOwner()
                    {
                        Id = id,
                        OwnerType = id.StartsWith("ch") ? OwnerType.Channel : OwnerType.User,
                        IconUrl = iconNode.GetAttribute("src"),
                        Nickname = userInfoNode.TextContent,
                    };
                }

                return seriesDetails;
            });
        }


        public Task<SeriesListResponse> GetUserSeriesAsync(NiconicoId userId, int page = 0, int pageSize = 100)
        {
            return _context.GetJsonAsAsync<SeriesListResponse>(
                $"{NiconicoUrls.NvApiV1Url}users/{userId}/series?page={page+1}&pageSize={pageSize}",
                _options
                );
        }
    }

    public class SeriesDetails
    {
        public SeriesItem Series { get; set; }
        public SeriesOwner Owner { get; set; }
        public string DescriptionHTML { get; set; }
        public List<SeriesVideo> Videos { get; set; }


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
            public OwnerType OwnerType { get; set; }
            public string Id { get; set; }
            public string Nickname { get; set; }
            public string IconUrl { get; set; }
        }

        public class SeriesSimple
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }



        public class SeriesItem
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public int? Count { get; set; }
            public Uri ThumbnailUrl { get; set; }
        }
    }

}

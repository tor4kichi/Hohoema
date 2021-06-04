using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiconicoToolkit.Rss.Video;
using Windows.Web.Syndication;

namespace NiconicoToolkit.Ranking.Video
{


    public sealed class VideoRankinguSubClient
    {
        private readonly NiconicoContext _context;

        public VideoRankinguSubClient(NiconicoContext context)
        {
            _context = context;
        }

        public static bool IsHotTopicAcceptTerm(RankingTerm term)
        {
            return VideoRankingConstants.HotTopicAccepteRankingTerms.Any(x => x == term);
        }

        public static bool IsGenreWithTagAcceptTerm(RankingTerm term)
        {
            return VideoRankingConstants.GenreWithTagAccepteRankingTerms.Any(x => x == term);
        }



        async Task<List<RankingGenrePickedTag>> Internal_GetPickedTagAsync(string url, bool isHotTopic, CancellationToken ct)
        {
            await _context.WaitPageAccess();

            IHtmlDocument doc;
            var parser = new HtmlParser();
            var res = await _context.GetAsync(url, ct: ct);
            using (var contentStream = await res.Content.ReadAsInputStreamAsync())
            using (doc = await parser.ParseDocumentAsync(contentStream.AsStreamForRead()))
            {
                // ページ上の .RankingFilterTag となる要素を列挙する
                var tagAnchorElements = isHotTopic 
                    ? doc.QuerySelectorAll(@"section.HotTopicsContainer > ul > li > a") 
                    : doc.QuerySelectorAll(@"section.RepresentedTagsContainer > ul > li > a") 
                    ;

                List<RankingGenrePickedTag> items = new ();
                foreach (var element in tagAnchorElements)
                {
                    var tag = new RankingGenrePickedTag();
                    tag.DisplayName = element.TextContent.Trim('\n', ' ');
                    var hrefAttr = element.GetAttribute("href");
                    var splited = hrefAttr.Split('=', '&');
                    var first = splited.ElementAtOrDefault(1);
                    tag.Tag = Uri.UnescapeDataString(first?.Trim('\n') ?? String.Empty);

                    items.Add(tag);
                }

                return items;
            }
        }

        /// <summary>
        /// 指定ジャンルの「人気のタグ」を取得します。 <br />
        /// RankingGenre.All を指定した場合のみ、常に空のListを返します。（RankingGenre.All は「人気のタグ」を指定できないため）
        /// </summary>
        /// <param name="genre">RankingGenre.All"以外"を指定します。</param>
        /// <remarks></remarks>
        /// <returns></returns>
        public async Task<List<RankingGenrePickedTag>> GetGenrePickedTagAsync(RankingGenre genre, CancellationToken ct = default)
        {
            if (genre == RankingGenre.All) { return new List<RankingGenrePickedTag>(); }

            if (genre != RankingGenre.HotTopic)
            {
                return await Internal_GetPickedTagAsync($"{VideoRankingConstants.NiconicoRankingGenreDomain}{genre.GetDescription()}", isHotTopic: false, ct);
            }
            else
            {
                return await Internal_GetPickedTagAsync($"{VideoRankingConstants.NiconicoRankingHotTopicDomain}", isHotTopic: true, ct);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="genre"></param>
        /// <param name="tag"></param>
        /// <param name="term"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<RssVideoResponse> GetRankingRssAsync(RankingGenre genre, string tag = null, RankingTerm term = RankingTerm.Hour, int page = 1, CancellationToken ct = default)
        {
            if (genre != RankingGenre.HotTopic)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return await Internal_GetRankingRssAsync(genre, term, page, ct);
                }
                else
                {
                    return await Internal_GetRankingRssWithTagAsync(genre, tag, term, page, ct);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return await Internal_GetHotTopicRankingRssAsync(term, page, ct);
                }
                else
                {
                    return await Internal_GetHotTopicRankingRssWithKeyAsync(tag, term, ct);
                }
            }
        }


        async Task<RssVideoResponse> Internal_GetRankingRssAsync(RankingGenre genre, RankingTerm term, int page, CancellationToken ct)
        {
            if (page == 0 || page > VideoRankingConstants.MaxPage)
            {
                throw new ArgumentOutOfRangeException($"out of range {nameof(page)}. inside btw from 1 to {VideoRankingConstants.MaxPage} in with tag.");
            }

            var dict = new NameValueCollection()
            {
                { "rss", "2.0" },
                { "lang", "ja-jp" }
            };

            dict.Add(nameof(term), term.GetDescription());
            if (page != 1)
            {
                dict.Add(nameof(page), page.ToString());
            }

            try
            {
                StringBuilder sb = new StringBuilder(VideoRankingConstants.NiconicoRankingGenreDomain);
                sb.Append(genre.GetDescription());
                return await GetRssVideoResponseAsync(dict.ToQueryString(sb).ToString(), ct);
            }
            catch
            {
                return new RssVideoResponse() { IsOK = false, Items = new List<RssVideoData>() };
            }
        }


        async Task<RssVideoResponse> Internal_GetRankingRssWithTagAsync(RankingGenre genre, string tag, RankingTerm term, int page, CancellationToken ct)
        {
            if (!IsGenreWithTagAcceptTerm(term))
            {
                throw new ArgumentOutOfRangeException($"out of range {nameof(RankingTerm)}. accept with {string.Join(" or ", VideoRankingConstants.GenreWithTagAccepteRankingTerms)}.");
            }

            if (page == 0 || page > VideoRankingConstants.MaxPageWithTag)
            {
                throw new ArgumentOutOfRangeException($"out of range {nameof(page)}. inside btw from 1 to {VideoRankingConstants.MaxPageWithTag} in with tag.");
            }

            var dict = new NameValueCollection()
            {
                { "rss", "2.0" },
                { "lang", "ja-jp" }
            };

            dict.Add(nameof(term), term.GetDescription());
            if (tag != null)
            {
                dict.Add(nameof(tag), tag);
            }
            if (page != 1)
            {
                dict.Add(nameof(page), page.ToString());
            }

            try
            {
                StringBuilder sb = new StringBuilder(VideoRankingConstants.NiconicoRankingGenreDomain);
                sb.Append(genre.GetDescription());
                return await GetRssVideoResponseAsync(dict.ToQueryString(sb).ToString(), ct);
            }
            catch
            {
                return new RssVideoResponse() { IsOK = false, Items = new List<RssVideoData>() };
            }
        }

        async Task<RssVideoResponse> Internal_GetHotTopicRankingRssAsync(RankingTerm term, int page, CancellationToken ct)
        {
            if (!IsHotTopicAcceptTerm(term))
            {
                throw new ArgumentOutOfRangeException($"out of range {nameof(RankingTerm)}. accept with {string.Join(" or ", VideoRankingConstants.HotTopicAccepteRankingTerms)}.");
            }

            if (page == 0 || page > VideoRankingConstants.MaxPageHotTopic)
            {
                throw new ArgumentOutOfRangeException($"out of range {nameof(page)}. inside btw from 1 to {VideoRankingConstants.MaxPageHotTopic} in with tag.");
            }

            var dict = new NameValueCollection()
            {
                { "rss", "2.0" },
                { "lang", "ja-jp" }
            };

            dict.Add(nameof(term), term.GetDescription());
            if (page != 1)
            {
                dict.Add(nameof(page), page.ToString());
            }

            try
            {
                StringBuilder sb = new StringBuilder(VideoRankingConstants.NiconicoRankingHotTopicDomain);
                return await GetRssVideoResponseAsync(dict.ToQueryString(sb).ToString(), ct);
            }
            catch
            {
                return new RssVideoResponse() { IsOK = false, Items = new List<RssVideoData>() };
            }
        }

        async Task<RssVideoResponse> Internal_GetHotTopicRankingRssWithKeyAsync(string key, RankingTerm term, CancellationToken ct)
        {
            if (!IsHotTopicAcceptTerm(term))
            {
                throw new ArgumentOutOfRangeException($"out of range {nameof(RankingTerm)}. accept with {string.Join(" or ", VideoRankingConstants.HotTopicAccepteRankingTerms)}.");
            }

            var dict = new NameValueCollection()
            {
                { "rss", "2.0" },
                { "lang", "ja-jp" }
            };

            dict.Add(nameof(key), key);
            dict.Add(nameof(term), term.GetDescription());

            try
            {
                StringBuilder sb = new StringBuilder(VideoRankingConstants.NiconicoRankingHotTopicDomain);
                return await GetRssVideoResponseAsync(dict.ToQueryString(sb).ToString(), ct);
            }
            catch
            {
                return new RssVideoResponse() { IsOK = false, Items = new List<RssVideoData>() };
            }
        }


        Windows.Web.Syndication.SyndicationClient _client = new Windows.Web.Syndication.SyndicationClient();

        private async Task<RssVideoResponse> GetRssVideoResponseAsync(string url, CancellationToken ct)
        {
            System.Diagnostics.Debug.WriteLine(url);

            var text = await _context.GetStringAsync(url);
            var feed = new SyndicationFeed();
            feed.Load(text);
            var items = new List<RssVideoData>();
            foreach (var item in feed.Items)
            {
                items.Add(new RssVideoData()
                {
                    RawTitle = item.Title.Text,
                    WatchPageUrl = item.Links[0].Uri,
                    Description = item.Summary.Text,
                    PubDate = item.PublishedDate
                });
            }

            return new RssVideoResponse()
            {
                IsOK = true,
                Items = items,
                Language = feed.Language,
                Link = feed.FirstUri
            };
        }
    }



}

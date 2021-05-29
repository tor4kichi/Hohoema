using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Video;
using NiconicoToolkit.Ranking.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Rss.Video;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class VideoTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _videoClient = _context.Video;
        }

        NiconicoContext _context;
        VideoClient _videoClient;


        #region Video Info
        [TestMethod]
        [DataRow("sm9")]
        [DataRow("so38721201")]
        public async Task GetVideoInfoAsync(string videoId)
        {
            var info = await _videoClient.GetVideoInfoAsync(videoId);

            Assert.IsTrue(info.IsOK);

            CheckVideo(info.Video);

            Assert.IsNotNull(info.Thread);
            Assert.AreNotEqual(info.Thread.Id, 0L);
        }

        [TestMethod]
        [DataRow("sm9", "so38721201")]
        public async Task GetVideoInfoManyAsync(string videoId1, string videoId2)
        {
            var ids = new[] { videoId1, videoId2 };
            var res = await _videoClient.GetVideoInfoManyAsync(ids);

            Assert.IsTrue(res.IsOK);
            Assert.AreEqual(ids.Length, (int)res.Videos.Count);

            foreach (var info in res.Videos)
            {
                CheckVideo(info.Video);

                Assert.IsNotNull(info.Thread);
                Assert.AreNotEqual(info.Thread.Id, 0L);
            }
        }




        private void CheckVideo(NiconicoToolkit.Video.Video video)
        {
            Assert.IsNotNull(video);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(video.Title));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(video.Description));
            Assert.AreNotEqual(video.LengthInSeconds, 0L);
            Assert.IsNotNull(video.ThumbnailUrl);
            Assert.AreNotEqual(video.FirstRetrieve, default(DateTimeOffset));

            if (video.ProviderType == VideoProviderType.Channel)
            {
                Assert.IsNotNull(video.CommunityId);
                Assert.AreNotEqual(video.CommunityId, "0");
            }
            else if (video.ProviderType == VideoProviderType.Regular)
            {
                Assert.IsNotNull(video.UserId);
                Assert.AreNotEqual(video.UserId, 0);
            }
        }

        #endregion VideoInfo


        #region Ranking

        [TestMethod]
        [DataRow(RankingGenre.All)]
        [DataRow(RankingGenre.HotTopic)]
        [DataRow(RankingGenre.Anime)]
        public async Task GetRankingTagsAsync(RankingGenre genre)
        {
            var res = await _context.Video.Ranking.GetGenrePickedTagAsync(genre);            
            foreach (var tag in res)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(tag.DisplayName));
                Assert.IsNotNull(tag.Tag);
            }

            if (genre is not RankingGenre.All)
            {
                Assert.AreEqual(res.Count(x => x.IsDefaultTag), 1);
            }
        }



        [TestMethod]
        [DataRow(RankingGenre.All)]
        [DataRow(RankingGenre.HotTopic)]
        [DataRow(RankingGenre.Anime)]
        public async Task GetRankingRssItemsAsync(RankingGenre genre)
        {
            var res = await _context.Video.Ranking.GetRankingRssAsync(genre);

            Assert.IsTrue(res.IsOK);
            Assert.IsTrue(res.Items.Any());

            foreach (var item in res.Items.Take(3))
            {
                Assert.IsNotNull(item.Description);
                Assert.IsNotNull(item.RawTitle);
                Assert.AreNotEqual(item.PubDate, default(DateTimeOffset));

                var moreData = item.GetMoreData();

                Assert.AreNotEqual(moreData.Length, default(TimeSpan));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(moreData.Title));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(moreData.ThumbnailUrl));
                Assert.IsTrue(moreData.WatchCount > 0);
            }
        }


        [TestMethod]
        [DataRow(RankingGenre.HotTopic)]
        [DataRow(RankingGenre.Anime)]
        public async Task GetRankingRssItemsWithTagAsync(RankingGenre genre)
        {
            var tagsRes = await _context.Video.Ranking.GetGenrePickedTagAsync(genre);

            var tag = tagsRes.Skip(1).FirstOrDefault();
            var res = await _context.Video.Ranking.GetRankingRssAsync(genre, tag.Tag);

            Assert.IsTrue(res.IsOK);
            Assert.IsTrue(res.Items.Any());

            foreach (var item in res.Items.Take(3))
            {
                Assert.IsNotNull(item.Description);
                Assert.IsNotNull(item.RawTitle);
                Assert.AreNotEqual(item.PubDate, default(DateTimeOffset));

                var moreData = item.GetMoreData();

                Assert.AreNotEqual(moreData.Length, default(TimeSpan));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(moreData.Title));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(moreData.ThumbnailUrl));
                Assert.IsTrue(moreData.WatchCount > 0);
            }
        }

        #endregion Ranking
    }
}

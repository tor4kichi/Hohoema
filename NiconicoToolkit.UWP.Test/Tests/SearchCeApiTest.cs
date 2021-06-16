using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class SearchCeApiTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
        }

        NiconicoContext _context;

        private void CheckVideo(VideoItem video)
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


        [TestMethod]
        [DataRow("sm9")]
        [DataRow("so38721201")]
        public async Task IdSingleSearchAsync(string videoId)
        {
            var info = await _context.SearchWithCeApi.Video.IdSearchAsync(videoId);

            Assert.IsTrue(info.IsOK);

            CheckVideo(info.Video);

            Assert.IsNotNull(info.Thread);
            Assert.AreNotEqual(info.Thread.Id, 0L);
        }

        [TestMethod]
        [DataRow("sm9", "so38721201")]
        public async Task IdSearchAsync(string videoId1, string videoId2)
        {
            var ids = new[] { (VideoId)videoId1, (VideoId)videoId2 };
            var res = await _context.SearchWithCeApi.Video.IdSearchAsync(ids);

            Assert.IsTrue(res.IsOK);
            Assert.AreEqual(ids.Length, res.Videos.Length);

            foreach (var info in res.Videos)
            {
                CheckVideo(info.Video);

                Assert.IsNotNull(info.Thread);
                Assert.AreNotEqual(info.Thread.Id, 0L);
            }
        }


        [TestMethod]
        [DataRow("WoWs")]
        public async Task KeywordSearchAsync(string keyword)
        {
            var res = await _context.SearchWithCeApi.Video.KeywordSearchAsync(keyword, 0, 3);
            Assert.IsTrue(res.IsOK);
            if (res.TotalCount > 0)
            {
                Assert.IsNotNull(res.Videos);
                Assert.IsTrue(res.Videos.Any());

                var sampleItem = res.Videos[0];
                CheckVideo(sampleItem.Video);

                Assert.IsNotNull(sampleItem.Thread);
                Assert.AreNotEqual(sampleItem.Thread.Id, 0L);
            }
        }


        [TestMethod]
        [DataRow("Splatoon2")]
        public async Task TagSearchAsync(string tag)
        {
            var res = await _context.SearchWithCeApi.Video.TagSearchAsync(tag, 0, 3);
            Assert.IsTrue(res.IsOK);
            if (res.TotalCount > 0)
            {
                Assert.IsNotNull(res.Videos);
                Assert.IsTrue(res.Videos.Any());

                var sampleItem = res.Videos[0];
                CheckVideo(sampleItem.Video);

                Assert.IsNotNull(sampleItem.Thread);
                Assert.AreNotEqual(sampleItem.Thread.Id, 0L);
            }
        }
    }
}

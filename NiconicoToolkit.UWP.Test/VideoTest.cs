using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test
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
    }
}

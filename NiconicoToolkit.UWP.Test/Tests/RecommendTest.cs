using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Recommend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class RecommendTest
    {
        NiconicoContext _context;

        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
        }


        void TestRecommendResponse(VideoRecommendResponse res)
        {
            Assert.IsTrue(res.Meta.IsSuccess);

            Assert.IsNotNull(res.Data.Recipe);
            Assert.IsNotNull(res.Data.RecommendId);

            if (res.Data.Items.FirstOrDefault(x => x.ContentType == Recommend.RecommendContentType.Video) is not null and var sampleVideoItem)
            {
                Assert.IsNotNull(sampleVideoItem.ContentAsVideo);
                var videoItem = sampleVideoItem.ContentAsVideo;
                Assert.IsNotNull(videoItem);

                Assert.IsNotNull(videoItem.Id);
                Assert.IsNotNull(videoItem.Title);
                Assert.IsNotNull(videoItem.Thumbnail);
                Assert.AreNotEqual(videoItem.RegisteredAt, default(DateTimeOffset));
            }

            

            if (res.Data.Items.FirstOrDefault(x => x.ContentType == Recommend.RecommendContentType.Mylist) is not null and var sampleMylistItem)
            {
                Assert.IsNotNull(sampleMylistItem.ContentAsMylist);
                var mylistItem = sampleMylistItem.ContentAsMylist;
                Assert.IsNotNull(mylistItem);

                Assert.IsNotNull(mylistItem.Id);
                Assert.IsNotNull(mylistItem.Name);
                Assert.IsNotNull(mylistItem.Owner);
                Assert.AreNotEqual(mylistItem.CreatedAt, default(DateTimeOffset));
            }
            
        }

        [TestMethod]
        [DataRow("sm38589779")]
        public async Task GetVideoRecommendAsync(string videoId)
        {
            var res = await _context.Recommend.GetVideoReccommendAsync(videoId);

            TestRecommendResponse(res);
        }

        [TestMethod]
        [DataRow("so38760676")]
        public async Task GetChannelVideoRecommendAsync(string videoId)
        {
            var video = await _context.SearchWithCeApi.Video.IdSearchAsync(videoId);            
            var res = await _context.Recommend.GetChannelVideoReccommendAsync(videoId, video.Video.CommunityId, video.Tags.TagInfo.Select(x => x.Tag));
            
            TestRecommendResponse(res);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.User;
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
            var res = await _context.Recommend.GetVideoRecommendForNotChannelAsync(videoId);

            TestRecommendResponse(res);
        }

        [TestMethod]
        [DataRow("so38760676")]
        public async Task GetVideoChannelRecommendAsync(string videoId)
        {
            var video = await _context.SearchWithCeApi.Video.IdSearchAsync(videoId);            
            var res = await _context.Recommend.GetVideoRecommendForChannelAsync(videoId, video.Video.CommunityId, video.Tags.TagInfo.Select(x => x.Tag));
            
            TestRecommendResponse(res);
        }


        [TestMethod]
        [DataRow("lv332342875", "91190464")]
        public async Task GetLiveUserRecommendAsync(string liveId, string userId)
        {
            var res = await _context.Recommend.GetLiveRecommendForUserAsync(liveId, userId);
            TestLiveRecommend(res);
        }


        [TestMethod]
        [DataRow("lv331311774", "ch2647027")]
        public async Task GetLiveChannelRecommendAsync(string liveId, string channelId)
        {
            var res = await _context.Recommend.GetLiveRecommendForChannelAsync(liveId, channelId);
            TestLiveRecommend(res);
        }

        private void TestLiveRecommend(LiveRecommendResponse res)
        {

            Assert.IsTrue(res.IsSuccess, "failed");

            Assert.IsNotNull(res.Data, "res.Data is null");
            Assert.IsNotNull(res.Data.RecipeId, "res.Data.RecipeId is null");
            Assert.IsNotNull(res.Data.RecommendId, "res.Data.RecommendId is null");
            Assert.IsNotNull(res.Data.Items, "res.Data.Values is null");

            if (res.Data.Items.Any())
            {
                foreach (var item in res.Data.Items.Take(3))
                {
                    Assert.IsNotNull(item.Id, "item.Id is null");
                    Assert.IsNotNull(item.ContentMeta, "item.ContentMeta is null");
                    Assert.IsNotNull(item.ContentMeta.ContentId, "item.ContentMeta.ContentId is null");
                    Assert.IsNotNull(item.ContentMeta.Title, "item.ContentMeta.Title is null");
                }
            }
        }

    }
}

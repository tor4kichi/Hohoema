using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class CommunityTest
    {
        NiconicoContext _context;


        [TestInitialize]
        public async Task Initialize()
        {
            (_context, _, _, _) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
        }


        [TestMethod]
        [DataRow("co540200")]
        [DataRow("5406776")]
        public async Task GetCommunityInfoAsync(string communityId)
        {
            var res = await _context.Community.GetCommunityInfoAsync(communityId);

            Assert.IsTrue(res.IsOK);

            Assert.IsNotNull(res.Community, "res.Community is null");
            Assert.AreNotEqual(default(UserId), res.Community.UserId, "res.Data.UserId is default");
        }

        [TestMethod]
        [DataRow("co540200")]
        [DataRow("5406776")]
        public async Task GetCommunityAuthorityForLoginUserAsync(string communityId)
        {
            var res = await _context.Community.GetCommunityAuthorityForLoginUserAsync(communityId);

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "res.Data is null");
            Assert.AreNotEqual(0, res.Data.UserId, "res.Data.user is default");
        }

        [TestMethod]
        [DataRow("co540200")]
        public async Task GetCommunityVideoAsync(string communityId)
        {
            var res = await _context.Community.GetCommunityVideoListAsync(communityId);

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "res.Data is null");
            Assert.IsNotNull(res.Data.Contents, "res.Data.Contents is null");

            if (res.Data.Contents.Any())
            {
                foreach (var item in res.Data.Contents.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine("CommunityContent.ContentKind" + item.ContentKind);

                    Assert.IsNotNull(item.ContentId, "item.ContentId is null");
                    Assert.AreNotEqual(0L, item.Id, "item.Id is default");
                    Assert.AreNotEqual(0L, item.CommunityId, "item.CommunityId is default");

                    Assert.AreNotEqual(default(DateTimeOffset), item.CreateTime);
                }
            }

            var videoItemsRes = await _context.Community.GetCommunityVideoListItemsAsync(res.Data.Contents.Select(x => x.ContentId));

            Assert.IsTrue(videoItemsRes.IsSuccess);
            Assert.IsNotNull(videoItemsRes.Data, "videoItemsRes.Data is null");
            Assert.IsNotNull(videoItemsRes.Data.Videos, "videoItemsRes.Data.Videos is null");

            foreach (var item in videoItemsRes.Data.Videos.Take(3))
            {
                Assert.IsNotNull(item.Id);
                Assert.IsNotNull(item.Title);
                Assert.IsNotNull(item.Description);

                item.GetCreateTime();
            }
        }


        [TestMethod]
        [DataRow("co540200")]
        public async Task GetCommunityLiveAsync(string communityId)
        {
            var res = await _context.Community.GetCommunityLiveAsync(communityId);

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "res.Data is null");
            Assert.IsNotNull(res.Data.Lives, "res.Data.Lives is null");

            if (res.Data.Lives.Any())
            {
                foreach (var item in res.Data.Lives.Take(3))
                {
                    Assert.IsNotNull(item.Id, "item.Id is null");
                    Assert.IsNotNull(item.Title, "item.Title is null");
                    Assert.IsNotNull(item.Description, "item.Description is null");
                }
            }
        }
    }
}

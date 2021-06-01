using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class FollowTest
    {
        private NiconicoContext _context;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var result = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
            _context = result.niconicoContext;
        }


        [TestMethod]
        public async Task GetFollowTagsAsync()
        {
            var res = await _context.Follow.Tag.GetFollowTagsAsync();
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "Data is null");
            Assert.IsNotNull(res.Data.Tags, "Data.Tags is null");
            
            if (res.Data.Tags.Any())
            {
                var tag = res.Data.Tags[0];
                Assert.AreNotEqual(default(DateTimeOffset), tag.FollowedAt);
                Assert.IsNotNull(tag.Name, "tag.Name is null");
                Assert.IsNotNull(tag.NicodicSummary, "tag.NicodicSummary is null");
            }
        }

        [TestMethod]
        public async Task GetFollowUsersAsync()
        {
            var res = await _context.Follow.User.GetFollowUsersAsync(3);
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "Data is null");
            Assert.IsNotNull(res.Data.Items, "Data.Items is null");

            if (res.Data.Items.Any())
            {
                var user = res.Data.Items[0];
                Assert.IsNotNull(user.Nickname, "user.Nickname is null");
                Assert.IsNotNull(user.Description, "tag.Description is null");
            }
        }


        [TestMethod]
        public async Task GetFollowMylistsAsync()
        {
            var res = await _context.Follow.Mylist.GetFollowMylistsAsync(0);
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "Data is null");
            Assert.IsNotNull(res.Data.Mylists, "Data.Mylists is null");

            if (res.Data.Mylists.Any())
            {
                var mylist = res.Data.Mylists[0];
                Assert.IsNotNull(mylist.Detail, "mylist.Detail is null");
                Assert.IsNotNull(mylist.Detail.Name, "mylist.Detail.Name is null");
                Assert.IsNotNull(mylist.Detail.Description, "mylist.Detail.Description is null");
            }
        }


        [TestMethod]
        public async Task GetFollowCommunitiesAsync()
        {
            var res = await _context.Follow.Community.GetFollowCommunityAsync();
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "Data is null");
            
            if (res.Data.Any())
            {
                var community = res.Data[0];
                Assert.IsNotNull(community.Name, "community.Name is null");
                Assert.IsNotNull(community.GlobalId, "community.GlobalId is null");
            }
        }


        [TestMethod]
        public async Task GetFollowChannelsAsync()
        {
            var res = await _context.Follow.Channel.GetFollowChannelAsync();
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "Data is null");

            if (res.Data.Any())
            {
                var channel = res.Data[0];
                Assert.IsNotNull(channel.Name, "channel.Name is null");
                Assert.IsNotNull(channel.ScreenName, "channel.ScreenName is null");
            }
        }
    }
}

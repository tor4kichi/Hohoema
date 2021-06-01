using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class VideoMylistLoginTest
    {
        private NiconicoContext _context;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var account = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
            _context = account.niconicoContext;
        }


        [TestMethod]
        public async Task GetMylistGroupsAsync()
        {
            var res = await _context.Mylist.LoginUser.GetMylistGroupsAsync();
            
            Assert.IsTrue(res.Meta.IsSuccess);
            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Mylists);

            if (res.Data.Mylists.Any())
            {
                var mylist = res.Data.Mylists[0];
                Assert.IsNotNull(mylist.Name);
                Assert.IsNotNull(mylist.Owner);
                Assert.IsNotNull(mylist.SampleItems);
            }
        }

        [TestMethod]
        public async Task GetWatchAfterMylistItemsAsync()
        {
            var res = await _context.Mylist.LoginUser.GetWatchAfterItemsAsync();

            Assert.IsTrue(res.Meta.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Mylist);
            Assert.IsNotNull(res.Data.Mylist.Items);

            if (res.Data.Mylist.Items.Any())
            {
                var item = res.Data.Mylist.Items[0];
                Assert.IsNotNull(item.WatchId);
                Assert.IsNotNull(item.Video);
                Assert.IsNotNull(item.Video.Title);
                Assert.IsNotNull(item.Video.Thumbnail);
            }
        }

        [TestMethod]
        public async Task GetLoginUserMylistItemsAsync()
        {
            var mylistsRes = await _context.Mylist.LoginUser.GetMylistGroupsAsync();
            var mylist = mylistsRes.Data.Mylists[0];
            var res = await _context.Mylist.LoginUser.GetMylistItemsAsync(mylist.Id.ToString());

            Assert.IsTrue(res.Meta.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Mylist);
            Assert.IsNotNull(res.Data.Mylist.Items);

            if (res.Data.Mylist.Items.Any())
            {
                var item = res.Data.Mylist.Items[0];
                Assert.IsNotNull(item.WatchId);
                Assert.IsNotNull(item.Video);
                Assert.IsNotNull(item.Video.Title);
                Assert.IsNotNull(item.Video.Thumbnail);
            }
        }

    }
}

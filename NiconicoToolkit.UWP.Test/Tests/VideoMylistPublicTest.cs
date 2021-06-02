using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class VideoMylistPublicTest
    {
        private NiconicoContext _context;

        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _context.SetupDefaultRequestHeaders();
        }

        [TestMethod]
        [DataRow("53842185")]
        public async Task GetUserMylistGroupsAsync(string userId)
        {
            var res = await _context.Mylist.GetUserMylistGroupsAsync(userId, sampleItemCount: 1);

            Assert.IsTrue(res.Meta.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.MylistGroups);
            
            if (res.Data.MylistGroups.Any())
            {
                var mylist = res.Data.MylistGroups[0];

                Assert.IsNotNull(mylist.Name);
                Assert.IsNotNull(mylist.Owner);
            }
        }


        [TestMethod]
        [DataRow("64876720")]
        [DataRow("56000990")]
        public async Task GetMylistItemsAsync(string mylistId)
        {
            var res = await _context.Mylist.GetMylistItemsAsync(mylistId);

            Assert.IsTrue(res.Meta.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Mylist);
            Assert.IsNotNull(res.Data.Mylist.Items);

            if (res.Data.Mylist.Items.Any())
            {
                var item = res.Data.Mylist.Items[0];

                Assert.IsNotNull(item.Video);
                Assert.IsNotNull(item.Video.Title);
                Assert.IsNotNull(item.Video.Owner);
            }
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class ChannelTest
    {
        private NiconicoContext _context;

        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _context.SetupDefaultRequestHeaders();
        }


        [TestMethod]
        //[DataRow("maidragon")]
        [DataRow("ch2647775")]
        [DataRow("2647798")]
        public async Task GetChannelInfoAsync(string channelId)
        {
            var res = await _context.Channel.GetChannelInfoAsync(channelId);
            Assert.IsTrue(res != null);

            Assert.IsNotNull(res.Name);
            Assert.IsNotNull(res.ScreenName);
            Assert.IsNotNull(res.CompanyViewname);
        }


        [TestMethod]
        [DataRow("maidragon")]
        [DataRow("ch2647775")]
        [DataRow("2647798")]
        public async Task GetChannelVideoAsync(string channelId)
        {
            var res = await _context.Channel.GetChannelVideoAsync(channelId, page: 0);
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Videos);
            
            if (res.Data.Videos.Any())
            {
                Assert.AreNotEqual(0, res.Data.TotalCount);

                var sampleItem = res.Data.Videos[0];
                Assert.IsNotNull(sampleItem.Title, "sampleItem.Title is null");
                Assert.IsNotNull(sampleItem.ItemId, "sampleItem.ItemId is null");
                Assert.IsNotNull(sampleItem.ThumbnailUrl, "sampleItem.ThumbnailUrl is null");
                Assert.AreNotEqual(default(DateTime), sampleItem.PostedAt, "sampleItem.PostedAt is default value");
                Assert.AreNotEqual(default(TimeSpan), sampleItem.Length, "sampleItem.PostedAt is default value");
            }
        }

    }
}

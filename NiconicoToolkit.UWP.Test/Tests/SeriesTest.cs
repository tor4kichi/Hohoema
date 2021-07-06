using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class SeriesTest
    {
        private NiconicoContext _context;

        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _context.SetupDefaultRequestHeaders();
        }


        [TestMethod]
        [DataRow("76015")] // ユーザーのシリーズ
        [DataRow("225544")] // chのシリーズ
        public async Task GetSeriesVideoAsync(string seriesId)
        {
            var res = await _context.Series.GetSeriesVideosAsync(seriesId);
            Assert.IsNotNull(res.Series);
            Assert.IsNotNull(res.Videos);

            foreach (var video in res.Videos.Take(3))
            {
                Assert.IsNotNull(video.Id, "video.Id is null");
                Assert.IsNotNull(video.Title, "video.Title is null");
                Assert.AreNotEqual(TimeSpan.Zero, video.Duration, "video.Duration is TimeSpan.Zero");
            }

            Assert.IsNotNull(res.Owner);
            Assert.IsNotNull(res.Owner.Id);
            Assert.IsNotNull(res.Owner.Nickname);
        }


        [TestMethod]
        [DataRow(53842185)] // ユーザー
        [DataRow(225544)] // chのシリーズ
        public async Task GetUserSeriesAsync(int userId)
        {
            var res = await _context.Series.GetUserSeriesAsync(userId);
            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Items);
        }
    }
}

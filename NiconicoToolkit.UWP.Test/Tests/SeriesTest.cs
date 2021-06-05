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
        }


        [TestMethod]
        [DataRow("76015")]
        public async Task GetSeriesAsync(string seriesId)
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
        }
    }
}

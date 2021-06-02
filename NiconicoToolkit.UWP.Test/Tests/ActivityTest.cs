using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class ActivityTest
    {
        NiconicoContext _context;


        [TestInitialize]
        public async Task Initialize()
        {
            (_context, _, _, _) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
        }

        [TestMethod]
        public async Task GetVideoWatchHitoryAsync()
        {
            var res = await _context.Activity.VideoWachHistory.GetWatchHistoryAsync(0, 100);

            Assert.IsTrue(res.Meta.IsSuccess);

            if (res.Data.Items.Length > 0)
            {
                var item = res.Data.Items[0];
                Assert.IsNotNull(item.Video);
                Assert.IsNotNull(item.WatchId);
                Assert.AreNotEqual(item.LastViewedAt, default(DateTimeOffset));
            }
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class LikesTest
    {
        NiconicoContext _context;


        [TestInitialize]
        public async Task Initialize()
        {
            (_context, _, _, _) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
        }

        [TestMethod]
        public async Task GetLoginUserLikesListAsync()
        {
            var res = await _context.Likes.GetLikesAsync(0, 20);

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "res.Data is null");
            Assert.IsNotNull(res.Data.Items, "res.Data.Items is null");
            Assert.IsNotNull(res.Data.PageInfo, "res.Data.PageInfo is null");

            foreach (var sampleItem in res.Data.Items.Take(3))
            {
                Assert.IsNotNull(sampleItem.Video, "sampleItem.Video is null");
                Assert.AreNotEqual(default(DateTimeOffset), sampleItem.LikedAt, "sampleItem.LikedAt are same as default(DateTimeOffset)");
            }
        }
    }
}

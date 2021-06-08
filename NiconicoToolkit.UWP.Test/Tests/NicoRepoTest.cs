using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    
    [TestClass]
    public sealed class NicoRepoTest
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
            var res = await _context.NicoRepo.GetLoginUserNicoRepoEntriesAsync(NicoRepo.NicoRepoType.All, NicoRepo.NicoRepoDisplayTarget.All);

            Assert.IsTrue(res.IsSuccess);

            if (res.Data.Length > 0)
            {
                foreach (var item in res.Data.Take(3))
                {
                    Assert.IsNotNull(item.Id);
                    Assert.IsNotNull(item.Title);
                    Assert.IsNotNull(item.Object);
                    Assert.IsNotNull(item.MuteContext);
                    Assert.AreNotEqual(default(DateTimeOffset), item.Updated);
                }
            }
        }
    }
}

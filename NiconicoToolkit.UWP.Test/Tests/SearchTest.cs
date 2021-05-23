using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Search.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class SearchTest
    {
        [TestInitialize]
        public async Task Initialize()
        {
            var creationResult = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
            _context = creationResult.niconicoContext;
            _searchClient = _context.Search;
        }

        NiconicoContext _context;
        Search.SearchClient _searchClient;


        [TestMethod]
        [DataRow("モンハン")]
        public async Task VideoKeywordSearchAsync(string keyword)
        {
            var res = await _searchClient.Video.CreateQueryBuilder()
                .KeywordSearchAsync(keyword);

            Assert.IsTrue(res.IsStatusOK);

            if (res.Count > 0)
            {
                var sampleItem = res.List[0];

                Assert.IsNotNull(sampleItem.Title);
                Assert.AreNotEqual(sampleItem.Length, TimeSpan.Zero);
                Assert.AreNotEqual(sampleItem.FirstRetrieve, default(DateTime));
            }
        }

        [TestMethod]
        [DataRow("モンハン")]
        public async Task VideoTagSearchAsync(string keyword)
        {
            var res = await _searchClient.Video.CreateQueryBuilder()
                .TagSearchAsync(keyword);

            Assert.IsTrue(res.IsStatusOK);

            if (res.Count > 0)
            {
                var sampleItem = res.List[0];

                Assert.IsNotNull(sampleItem.Title);
                Assert.AreNotEqual(sampleItem.Length, TimeSpan.Zero);
                Assert.AreNotEqual(sampleItem.FirstRetrieve, default(DateTime));
            }
        }
    }
}

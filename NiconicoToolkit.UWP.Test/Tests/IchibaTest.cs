using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class IchibaTest
    {
        private NiconicoContext _context;

        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _context.SetupDefaultRequestHeaders();
        }


        
        [TestMethod]
        [DataRow("sm38672563")]
        [DataRow("so38835896")]
        public async Task GetIchibaItemAsync(string videoId)
        {
            var res = await _context.Ichiba.GetIchibaItemsAsync(videoId);

            Assert.IsNotNull(res.MainItems);
        }
    }
}

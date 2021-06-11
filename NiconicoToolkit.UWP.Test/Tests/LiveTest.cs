using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class LiveTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _liveClient = _context.Live;
        }

        NiconicoContext _context;
        LiveClient _liveClient;


        [TestMethod]
        [DataRow("lv331925323")]
        public async Task CasApi_GetLiveProgramAsync(string liveId)
        {
            var res = await _liveClient.CasApi.GetLiveProgramAsync(liveId);

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.Meta);
            Assert.IsTrue(res.Meta.Status == 200);

            Assert.IsNotNull(res.Data);
            var data = res.Data;
            Assert.IsTrue(!string.IsNullOrWhiteSpace(data.Description));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(data.ProviderId));

            Assert.IsNotNull(data.ThumbnailUrl);
            Assert.IsNotNull(data.Timeshift);
        }

        [TestMethod]
        [DataRow("lv331925323")]
        public async Task GetLiveWatchDataPropAsync(string liveId)
        {
            var res = await _liveClient.GetLiveWatchPageDataPropAsync(liveId);
        }

    }
}

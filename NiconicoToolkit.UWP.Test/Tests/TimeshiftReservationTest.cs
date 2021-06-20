using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class TimeshiftReservationTest
    {
        NiconicoContext _context;


        [TestInitialize]
        public async Task Initialize()
        {
            (_context, _, _, _) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
        }


        [TestMethod]
        public async Task GetTimeshiftReservationsDetailAsync()
        {
            var res = await _context.Timeshift.GetTimeshiftReservationsDetailAsync();

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Items);

            foreach (var item in res.Data.Items.Take(3))
            {
                Assert.IsNotNull(item.LiveId, "item.Id is null");
                Assert.IsNotNull(item.Title, "item.Title is null");
                Assert.IsNotNull(item.Status, "item.StatusText is null");
            }
        }

        [TestMethod]
        public async Task GetTimeshiftReservationsAsync()
        {
            var res = await _context.Timeshift.GetTimeshiftReservationsAsync();

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.ReservationToken);
            Assert.IsNotNull(res.Data.Items);

            foreach (var item in res.Data.Items.Take(3))
            {
                Assert.IsNotNull(item.Id, "item.Id is null");
                Assert.IsNotNull(item.Title, "item.Title is null");
                Assert.IsNotNull(item.StatusText, "item.StatusText is null");
            }
        }
    }
}

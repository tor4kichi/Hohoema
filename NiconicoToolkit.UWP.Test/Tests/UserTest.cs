using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class UserTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
            _context.SetupDefaultRequestHeaders();
            _userClient = _context.User;
        }

        NiconicoContext _context;
        UserClient _userClient;

        [TestMethod]
        [DataRow(53842185u)]
        public async Task GetUserInfoAsync(uint userId)
        {
            var res = await _userClient.GetUserInfoAsync(userId);
        }

        [TestMethod]
        [DataRow(53842185u)]
        public async Task GetUserNickNameAsync(uint userId)
        {
            var res = await _userClient.GetUserNicknameAsync(userId);
        }


        [TestMethod]
        [DataRow(53842185u)]
        public async Task GetUserVideoAsync(uint userId)
        {
            var res = await _userClient.GetUserVideoAsync(userId);
            
            Assert.IsTrue(res.IsSuccess);
            Assert.IsNotNull(res.Data);
            Assert.IsNotNull(res.Data.Items);
            if (res.Data.Items.Any())
            {
                Assert.AreNotEqual(0L, res.Data.TotalCount);

                var sample = res.Data.Items[0];
                Assert.IsNotNull(sample.Essential, "sample.Essential is null");
                Assert.IsNotNull(sample.Essential.Title, "sample.Essential.Title is null");
            }
        }
    }
}

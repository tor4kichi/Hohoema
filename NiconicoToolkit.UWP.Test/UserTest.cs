using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test
{
    [TestClass]
    public sealed class UserTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
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
    }
}

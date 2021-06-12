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

            Assert.AreNotEqual(default(UserId), res.Id, "res.Id is default(UserId)");
            Assert.IsNotNull(res.Nickname, "res.Nickname is null");
        }


        [TestMethod]
        [DataRow(53842185u)] // チャンネルを持たないユーザー
        [DataRow(6982981u)] // チャンネル保持したユーザー
        public async Task GetUserDetailAsync(uint userId)
        {
            var res = await _userClient.GetUserDetailAsync(userId);

            Assert.IsTrue(res.IsSuccess);

            Assert.IsNotNull(res.Data, "res.Data is null");
            Assert.IsNotNull(res.Data.User, "res.Data.User is null");
            Assert.IsNotNull(res.Data.User.Nickname, "res.Data.User.Nickname is null");
            Assert.IsNotNull(res.Data.User.Icons, "res.Data.User.Icons is null");
            Assert.IsNotNull(res.Data.User.Icons.Small, "res.Data.User.Icons.Small is null");
            Assert.IsNotNull(res.Data.User.Icons.Large, "res.Data.User.Icons.Large is null");
            Assert.IsNotNull(res.Data.User.Description, "res.Data.User.Description is null");
            Assert.IsNotNull(res.Data.FollowStatus, "res.Data.FollowStatus is null");
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


        [TestMethod]
        [DataRow(53842185, 6982981)] 
        public async Task GetUsersAsync(int userId1, int userId2)
        {
            var res = await _userClient.GetUsersAsync(new[] { userId1, userId2 });

            Assert.IsTrue(res.IsSuccess);

            foreach (var user in res.Data)
            {
                Assert.IsNotNull(user, "res.Data[0] is null");
                Assert.IsNotNull(user.Nickname, "user.Nickname is null");
                Assert.IsNotNull(user.Icons?.Urls, "user.Icons.Urls is null");
                Assert.IsNotNull(user.Description, "user.Description is null");
            }
        }
    }
}

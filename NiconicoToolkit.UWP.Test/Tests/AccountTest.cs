
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace NiconicoToolkit.UWP.Test.Tests
{
    

    [TestClass]
    public class AccountTest
    {
        NiconicoContext _context;
        uint _loginUserId;

        [TestInitialize]
        public async Task LogInAsync()
        {
            var (context, status, authority, userId) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();

            _context = context;
            _loginUserId = userId;
        }

        [TestCleanup]
        public async Task LogOutAsync()
        {
            var signOutResult = await _context.Account.SignOutAsync();
        }

        [TestMethod]
        public async Task GetLoginUserNameAsync()
        {
            var info = await _context.User.GetUserNicknameAsync(_loginUserId);

            Assert.IsNotNull(info.Nickname);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(info.Nickname));
        }
    }
}

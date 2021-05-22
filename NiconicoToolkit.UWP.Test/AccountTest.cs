
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace NiconicoToolkit.UWP.Test
{
    

    [TestClass]
    public class AccountTest
    {        
        [TestMethod]
        public async Task LogInAndLogOutAsync()
        {
            var (context, status, authority, userId) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();

            Assert.AreEqual(status, Account.NiconicoSessionStatus.Success);
            Assert.AreNotEqual(authority, Account.NiconicoAccountAuthority.NotSignedIn);
            Assert.AreNotEqual(userId, 0);

            var signOutResult = await context.Account.SignOutAsync();
            Assert.AreEqual(signOutResult, true);
        }
    }
}

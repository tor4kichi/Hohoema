using NiconicoToolkit.Account;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace NiconicoToolkit.UWP.Test
{
    public class AccountInfo
    {
        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

    }

    public static class AccountTestHelper 
    {
        public static async Task<AccountInfo> AccountLoadingAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/_TestAccount.json"));
            using (var stream = (await file.OpenSequentialReadAsync()).AsStreamForRead())
            {
                return await JsonSerializer.DeserializeAsync<AccountInfo>(stream);
            }
        }

        public static async Task<(NiconicoSessionStatus status, NiconicoAccountAuthority authority, uint userId)> LogInWithTestAccountAsync(NiconicoContext niconicoContext)
        {
            var accountInfo = await AccountLoadingAsync();
            return await niconicoContext.Account.SignInAsync(new MailAndPasswordAuthToken(accountInfo.Mail, accountInfo.Password));
        }

        public static async Task<(NiconicoContext niconicoContext, NiconicoSessionStatus status, NiconicoAccountAuthority authority, uint userId)> CreateNiconicoContextAndLogInWithTestAccountAsync()
        {
            NiconicoContext niconicoContext = new NiconicoContext("HohoemaTest");
            var accountInfo = await AccountLoadingAsync();
            var res = await niconicoContext.Account.SignInAsync(new MailAndPasswordAuthToken(accountInfo.Mail, accountInfo.Password));
            return (niconicoContext:  niconicoContext, res.status, res.authority, res.userId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    // Xboxとそれ以外でアカウント管理の方法を切り替えます
    // PCやモバイルでは Windows.Security.Credentials を利用し、
    // XboxOneでは 暗号化してローカルストレージに保存します。

    public static class AccountManager 
    {
        // v0.3.9 以前との互換性のために残しています
        const string RECENT_LOGIN_ACCOUNT = "recent_login_account";

        const string PRIMARY_ACCOUNT = "primary_account";

        const string XBOX_ACCOUNT_PASSWORD_CONTAINER = "xbox_account";
        public static Uri XboxEncryptKeyFileUri = new Uri("ms-appx:///Assets/xbox_encrypt_key.txt");

#if DEBUG
        const bool IsDebugXboxMode = false;

#endif


        private static async Task<string> GetXboxEncryptKey()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(XboxEncryptKeyFileUri);
            return await FileIO.ReadTextAsync(file);
        }


        public static void LoadRecentLoginAccount()
        {
            if (!Helpers.DeviceTypeHelper.IsXbox)
            {
                var vault = new Windows.Security.Credentials.PasswordVault();

                // v0.3.9 以前との互換性
                if (ApplicationData.Current.LocalSettings.Containers.ContainsKey(RECENT_LOGIN_ACCOUNT))
                {
                    var container = ApplicationData.Current.LocalSettings.Containers[RECENT_LOGIN_ACCOUNT];
                    var prop = container.Values.FirstOrDefault();

                    var id = prop.Key;
                    var password = prop.Value as string ?? "";

                    try
                    {
                        AddOrUpdateAccount(id, password);
                    }
                    catch { }

                    ApplicationData.Current.LocalSettings.DeleteContainer(RECENT_LOGIN_ACCOUNT);

                    SetPrimaryAccountId(id);
                }
            }
        }

        public static void SetPrimaryAccountId(string mailAddress)
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
            container.Values["primary_id"] = mailAddress;
        }

        public static string GetPrimaryAccountId()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
            return container.Values["primary_id"] as string;
        }

        public static bool HasPrimaryAccount()
        {
            try
            {
                var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
                return !string.IsNullOrWhiteSpace(container.Values["primary_id"] as string);
            }
            catch
            {
                return false;
            }
        }

        public static Task AddOrUpdateAccount(string mailAddress, string password)
        {
#if !DEBUG
            if (!Helpers.DeviceTypeHelper.IsXbox)
#else
            if (!IsDebugXboxMode && !Helpers.DeviceTypeHelper.IsXbox)
#endif
            {
                _AddOrUpdateAccount(mailAddress, password);
                return Task.CompletedTask;
            }
            else
            {
                return _AddOrUpdateAccount_Xbox(mailAddress, password);
            }
        }


        private static void _AddOrUpdateAccount(string mailAddress, string password)
        {
            var id = mailAddress;

            if (String.IsNullOrWhiteSpace(mailAddress) || String.IsNullOrWhiteSpace(password))
            {
                throw new Exception();
            }

            var vault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var credential = vault.Retrieve(nameof(HohoemaApp), id);
                vault.Remove(credential);
            }
            catch
            {
            }

            {
                var credential = new Windows.Security.Credentials.PasswordCredential(nameof(HohoemaApp), id, password);
                vault.Add(credential);
            }
        }

        private static async Task _AddOrUpdateAccount_Xbox(string mailAddress, string password)
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(XBOX_ACCOUNT_PASSWORD_CONTAINER, ApplicationDataCreateDisposition.Always);
            var encryptKey = await GetXboxEncryptKey();
            var encryptedPassword = Helpers.EncryptHelper.EncryptStringHelper(password, encryptKey);
            foreach (var pair in container.Values.ToArray())
            {
                var mail = pair.Key;
                if (mail == mailAddress && container.Values.ContainsKey(mail))
                {
                    container.Values.Remove(mail);
                    break;
                }
            }

            container.Values.Add(mailAddress, encryptedPassword);
        }

        public static bool RemoveAccount(string mailAddress)
        {
#if !DEBUG
            if (!Helpers.DeviceTypeHelper.IsXbox)
#else
            if (!IsDebugXboxMode && !Helpers.DeviceTypeHelper.IsXbox)
#endif
            {
                return _RemoveAccount(mailAddress);
            }
            else
            {
                return _RemoveAccount_Xbox(mailAddress);
            }
        }

        private static bool _RemoveAccount(string mailAddress)
        {
            var id = mailAddress;

            if (String.IsNullOrWhiteSpace(mailAddress))
            {
                return false;
            }

            var vault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var credential = vault.Retrieve(nameof(HohoemaApp), id);
                vault.Remove(credential);
                return true;
            }
            catch
            {
            }

            return false;
        }


        private static bool _RemoveAccount_Xbox(string mailAddress)
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(XBOX_ACCOUNT_PASSWORD_CONTAINER, ApplicationDataCreateDisposition.Always);
            foreach (var pair in container.Values.ToArray())
            {
                var mail = pair.Key;
                if (mailAddress == mail && container.Values.ContainsKey(mail))
                {
                    return container.Values.Remove(mail);
                }
            }

            return false;
        }


        public static Task<Tuple<string, string>> GetPrimaryAccount()
        {
            if (HasPrimaryAccount())
            {
#if !DEBUG
                if (!Helpers.DeviceTypeHelper.IsXbox)
#else
                if (!IsDebugXboxMode && !Helpers.DeviceTypeHelper.IsXbox)
#endif
                {
                    return Task.FromResult(_GetPrimaryAccount());
                }
                else
                {
                    return _GetPrimaryAccount_Xbox();
                }
            }
            else
            {
                return Task.FromResult<Tuple<string, string>>(null);
            }
        }


        public static Tuple<string, string> _GetPrimaryAccount()
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var primary_id = GetPrimaryAccountId();

                if (string.IsNullOrWhiteSpace(primary_id)) { return null; }

                var credential = vault.Retrieve(nameof(HohoemaApp), primary_id);
                credential.RetrievePassword();
                return new Tuple<string, string>(credential.UserName, credential.Password);
            }
            catch { }
            return null;
        }

        public static async Task<Tuple<string, string>> _GetPrimaryAccount_Xbox()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(XBOX_ACCOUNT_PASSWORD_CONTAINER, ApplicationDataCreateDisposition.Always);
            var encryptKey = await GetXboxEncryptKey();
            foreach (var pair in container.Values)
            {
                var mail = pair.Key;
                var encryptedPassword = (string)pair.Value;
                var plainPassword = Helpers.EncryptHelper.DecryptStringHelper(encryptedPassword, encryptKey);

                return new Tuple<string, string>(mail, plainPassword);
            }

            return null;
        }
    }


}

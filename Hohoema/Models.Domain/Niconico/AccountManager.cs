using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.Helpers
{
    // Xboxとそれ以外でアカウント管理の方法を切り替えます
    // PCやモバイルでは Windows.Security.Credentials を利用し、
    // XboxOneでは 暗号化してローカルストレージに保存します。

    public static class AccountManager 
    {
        const string AccountResrouceKey = "HohoemaApp";

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
            if (!DeviceTypeHelper.IsXbox)
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

        public static async ValueTask AddOrUpdateAccount(string mailAddress, string password)
        {
#if !DEBUG
            if (!DeviceTypeHelper.IsXbox)
#else
            if (!IsDebugXboxMode && !DeviceTypeHelper.IsXbox)
#endif
            {
                _AddOrUpdateAccount(mailAddress, password);
            }
            else
            {
                await _AddOrUpdateAccount_Xbox(mailAddress, password);
            }
        }


        private static void _AddOrUpdateAccount(string mailAddress, string password)
        {
            var id = mailAddress;

            if (String.IsNullOrWhiteSpace(mailAddress) || String.IsNullOrWhiteSpace(password))
            {
                throw new Models.Infrastructure.HohoemaExpception();
            }

            var vault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var credential = vault.Retrieve(AccountResrouceKey, id);
                vault.Remove(credential);
            }
            catch
            {
            }

            {
                var credential = new Windows.Security.Credentials.PasswordCredential(AccountResrouceKey, id, password);
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
            if (!DeviceTypeHelper.IsXbox)
#else
            if (!IsDebugXboxMode && !DeviceTypeHelper.IsXbox)
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
                var credential = vault.Retrieve(AccountResrouceKey, id);
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


        public static async ValueTask<Tuple<string, string>> GetPrimaryAccount()
        {
            if (HasPrimaryAccount())
            {
#if !DEBUG
                if (!DeviceTypeHelper.IsXbox)
#else
                if (!IsDebugXboxMode && !DeviceTypeHelper.IsXbox)
#endif
                {
                    return _GetPrimaryAccount();
                }
                else
                {
                    return await _GetPrimaryAccount_Xbox();
                }
            }
            else
            {
                return null;
            }
        }


        public static Tuple<string, string> _GetPrimaryAccount()
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var primary_id = GetPrimaryAccountId();

                if (string.IsNullOrWhiteSpace(primary_id)) { return null; }

                var credential = vault.Retrieve(AccountResrouceKey, primary_id);
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

namespace Hohoema.Models.Helpers
{
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage.Streams;

    // this code copy from
    // http://www.c-sharpcorner.com/article/encrypt-and-decrypt-string-in-windows-runtime-apps/
    public static class EncryptHelper
    {
        public static string EncryptStringHelper(string plainString, string key)
        {
            var hashKey = GetMD5Hash(key);
            var decryptBuffer = CryptographicBuffer.ConvertStringToBinary(plainString, BinaryStringEncoding.Utf8);
            var AES = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            var symmetricKey = AES.CreateSymmetricKey(hashKey);
            var encryptedBuffer = CryptographicEngine.Encrypt(symmetricKey, decryptBuffer, null);
            var encryptedString = CryptographicBuffer.EncodeToBase64String(encryptedBuffer);
            return encryptedString;
        }

        public static string DecryptStringHelper(string encryptedString, string key)
        {
            var hashKey = GetMD5Hash(key);
            IBuffer decryptBuffer = CryptographicBuffer.DecodeFromBase64String(encryptedString);
            var AES = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            var symmetricKey = AES.CreateSymmetricKey(hashKey);
            var decryptedBuffer = CryptographicEngine.Decrypt(symmetricKey, decryptBuffer, null);
            string decryptedString = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);
            return decryptedString;
        }

        private static IBuffer GetMD5Hash(string key)
        {
            IBuffer bufferUTF8Msg = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider hashAlgorithmProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashBuffer = hashAlgorithmProvider.HashData(bufferUTF8Msg);
            if (hashBuffer.Length != hashAlgorithmProvider.HashLength)
            {
                throw new Models.Infrastructure.HohoemaExpception("There was an error creating the hash");
            }
            return hashBuffer;
        }
    }
}
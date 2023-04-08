using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Hohoema.Helpers
{
    // Xboxとそれ以外でアカウント管理の方法を切り替えます
    // PCやモバイルでは Windows.Security.Credentials を利用し、
    // XboxOneでは 暗号化してローカルストレージに保存します。

    public static class AccountManager
    {
        private const string AccountResrouceKey = "HohoemaApp";

        // v0.3.9 以前との互換性のために残しています
        private const string RECENT_LOGIN_ACCOUNT = "recent_login_account";
        private const string PRIMARY_ACCOUNT = "primary_account";
        private const string XBOX_ACCOUNT_PASSWORD_CONTAINER = "xbox_account";
        public static Uri XboxEncryptKeyFileUri = new("ms-appx:///Assets/xbox_encrypt_key.txt");

#if DEBUG
        private const bool IsDebugXboxMode = false;

#endif


        private static async Task<string> GetXboxEncryptKey()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(XboxEncryptKeyFileUri);
            return await FileIO.ReadTextAsync(file);
        }


        public static void LoadRecentLoginAccount()
        {
            if (!DeviceTypeHelper.IsXbox)
            {
                _ = new Windows.Security.Credentials.PasswordVault();

                // v0.3.9 以前との互換性
                if (ApplicationData.Current.LocalSettings.Containers.ContainsKey(RECENT_LOGIN_ACCOUNT))
                {
                    ApplicationDataContainer container = ApplicationData.Current.LocalSettings.Containers[RECENT_LOGIN_ACCOUNT];
                    System.Collections.Generic.KeyValuePair<string, object> prop = container.Values.FirstOrDefault();

                    string id = prop.Key;
                    string password = prop.Value as string ?? "";

                    try
                    {
                        _ = AddOrUpdateAccount(id, password);
                    }
                    catch { }

                    ApplicationData.Current.LocalSettings.DeleteContainer(RECENT_LOGIN_ACCOUNT);

                    SetPrimaryAccountId(id);
                }
            }
        }

        public static void SetPrimaryAccountId(string mailAddress)
        {
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
            container.Values["primary_id"] = mailAddress;
        }

        public static string GetPrimaryAccountId()
        {
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
            return container.Values["primary_id"] as string;
        }

        public static bool HasPrimaryAccount()
        {
            try
            {
                ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
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
            string id = mailAddress;

            if (string.IsNullOrWhiteSpace(mailAddress) || string.IsNullOrWhiteSpace(password))
            {
                throw new Infra.HohoemaException();
            }

            Windows.Security.Credentials.PasswordVault vault = new();
            try
            {
                Windows.Security.Credentials.PasswordCredential credential = vault.Retrieve(AccountResrouceKey, id);
                vault.Remove(credential);
            }
            catch
            {
            }

            {
                Windows.Security.Credentials.PasswordCredential credential = new(AccountResrouceKey, id, password);
                vault.Add(credential);
            }
        }

        private static async Task _AddOrUpdateAccount_Xbox(string mailAddress, string password)
        {
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(XBOX_ACCOUNT_PASSWORD_CONTAINER, ApplicationDataCreateDisposition.Always);
            string encryptKey = await GetXboxEncryptKey();
            string encryptedPassword = Helpers.EncryptHelper.EncryptStringHelper(password, encryptKey);
            foreach (System.Collections.Generic.KeyValuePair<string, object> pair in container.Values.ToArray())
            {
                string mail = pair.Key;
                if (mail == mailAddress && container.Values.ContainsKey(mail))
                {
                    _ = container.Values.Remove(mail);
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
            string id = mailAddress;

            if (string.IsNullOrWhiteSpace(mailAddress))
            {
                return false;
            }

            Windows.Security.Credentials.PasswordVault vault = new();
            try
            {
                Windows.Security.Credentials.PasswordCredential credential = vault.Retrieve(AccountResrouceKey, id);
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
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(XBOX_ACCOUNT_PASSWORD_CONTAINER, ApplicationDataCreateDisposition.Always);
            foreach (System.Collections.Generic.KeyValuePair<string, object> pair in container.Values.ToArray())
            {
                string mail = pair.Key;
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
            Windows.Security.Credentials.PasswordVault vault = new();
            try
            {
                string primary_id = GetPrimaryAccountId();

                if (string.IsNullOrWhiteSpace(primary_id)) { return null; }

                Windows.Security.Credentials.PasswordCredential credential = vault.Retrieve(AccountResrouceKey, primary_id);
                credential.RetrievePassword();
                return new Tuple<string, string>(credential.UserName, credential.Password);
            }
            catch { }
            return null;
        }

        public static async Task<Tuple<string, string>> _GetPrimaryAccount_Xbox()
        {
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings.CreateContainer(XBOX_ACCOUNT_PASSWORD_CONTAINER, ApplicationDataCreateDisposition.Always);
            string encryptKey = await GetXboxEncryptKey();
            foreach (System.Collections.Generic.KeyValuePair<string, object> pair in container.Values)
            {
                string mail = pair.Key;
                string encryptedPassword = (string)pair.Value;
                string plainPassword = Helpers.EncryptHelper.DecryptStringHelper(encryptedPassword, encryptKey);

                return new Tuple<string, string>(mail, plainPassword);
            }

            return null;
        }
    }
}

namespace Hohoema.Helpers
{
    // this code copy from
    // http://www.c-sharpcorner.com/article/encrypt-and-decrypt-string-in-windows-runtime-apps/
    public static class EncryptHelper
    {
        public static string EncryptStringHelper(string plainString, string key)
        {
            IBuffer hashKey = GetMD5Hash(key);
            IBuffer decryptBuffer = CryptographicBuffer.ConvertStringToBinary(plainString, BinaryStringEncoding.Utf8);
            SymmetricKeyAlgorithmProvider AES = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            CryptographicKey symmetricKey = AES.CreateSymmetricKey(hashKey);
            IBuffer encryptedBuffer = CryptographicEngine.Encrypt(symmetricKey, decryptBuffer, null);
            string encryptedString = CryptographicBuffer.EncodeToBase64String(encryptedBuffer);
            return encryptedString;
        }

        public static string DecryptStringHelper(string encryptedString, string key)
        {
            IBuffer hashKey = GetMD5Hash(key);
            IBuffer decryptBuffer = CryptographicBuffer.DecodeFromBase64String(encryptedString);
            SymmetricKeyAlgorithmProvider AES = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            CryptographicKey symmetricKey = AES.CreateSymmetricKey(hashKey);
            IBuffer decryptedBuffer = CryptographicEngine.Decrypt(symmetricKey, decryptBuffer, null);
            string decryptedString = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);
            return decryptedString;
        }

        private static IBuffer GetMD5Hash(string key)
        {
            IBuffer bufferUTF8Msg = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider hashAlgorithmProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashBuffer = hashAlgorithmProvider.HashData(bufferUTF8Msg);
            return hashBuffer.Length != hashAlgorithmProvider.HashLength
                ? throw new Infra.HohoemaException("There was an error creating the hash")
                : hashBuffer;
        }
    }
}
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

        public static void LoadRecentLoginAccount()
        {
            if (!Util.DeviceTypeHelper.IsXbox)
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
            var container = ApplicationData.Current.LocalSettings.CreateContainer(PRIMARY_ACCOUNT, ApplicationDataCreateDisposition.Always);
            return container.Values["primary_id"] as string != null;
        }

        public static void AddOrUpdateAccount(string mailAddress, string password)
        {
            if (!Util.DeviceTypeHelper.IsXbox)
            {
                _AddOrUpdateAccount(mailAddress, password);
            }
            else
            {
                _AddOrUpdateAccount_Xbox(mailAddress, password);
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

        private static void _AddOrUpdateAccount_Xbox(string mailAddress, string password)
        {
            // TODO: 
        }

        public static void RemoveAccount(string mailAddress)
        {
            if (!Util.DeviceTypeHelper.IsXbox)
            {
                _RemoveAccount(mailAddress);
            }
            else
            {
                _RemoveAccount_Xbox(mailAddress);
            }
        }

        private static void _RemoveAccount(string mailAddress)
        {
            var id = mailAddress;

            if (String.IsNullOrWhiteSpace(mailAddress))
            {
                return;
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
        }


        private static void _RemoveAccount_Xbox(string mailAddress)
        {
            // TODO: 
        }


        public static Tuple<string, string> GetPrimaryAccount()
        {
            if (HasPrimaryAccount())
            {
                if (!Util.DeviceTypeHelper.IsXbox)
                {
                    return _GetPrimaryAccount();
                }
                else
                {
                    return _GetPrimaryAccount_Xbox();
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
                var credential = vault.Retrieve(nameof(HohoemaApp), primary_id);
                credential.RetrievePassword();
                return new Tuple<string, string>(credential.UserName, credential.Password);
            }
            catch { }
            return null;
        }

        public static Tuple<string, string> _GetPrimaryAccount_Xbox()
        {
            // TODO: 
            return null;
        }
    }


}

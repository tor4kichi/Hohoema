using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Hohoema.Models.Domain.Application
{
    public static class AppUpdateNotice
    {
        // TODO: 未チェックの更新ファイルの取得
        // TODO: チェック済み更新の保存と取得

        // 更新ファイルは 00-00-00.mdの形式で保存する
        // チェックしたバージョンをApplicationDataのLocalSettings.

#if DEBUG
        private static bool __ForceNoticeIsMinorVersionUpdated = false;
        private static bool __ForceNoticeUpdate = true;
#endif

        private static List<Version> _UpdateNoticeAvairableVersions;

        private static readonly AsyncLock _LoadLock = new AsyncLock();

       

        public static bool IsMinorVersionUpdated
        {
            get
            {
#if DEBUG
                if (__ForceNoticeIsMinorVersionUpdated) { return true; }
#endif
                var currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
                var currentAppVersion = new Version(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);
                return currentAppVersion.Minor > LastCheckedVersion.Minor;
            }
        }

        public static bool IsUpdated
        {
            get
            {
#if DEBUG
                if (__ForceNoticeUpdate) { return true; }
#endif
                var currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
                var currentAppVersion = new Version(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);
                return currentAppVersion > LastCheckedVersion;
            }
        }

        static Version _LastCheckedVersion;
        public static Version LastCheckedVersion
        {
            get
            {
                if (_LastCheckedVersion == null)
                {
                    try
                    {
                        var versionText = (string)ApplicationData.Current.LocalSettings.Values["update_notified_version"];

                        if (!string.IsNullOrEmpty(versionText))
                        {
                            _LastCheckedVersion = Version.Parse(versionText);
                        }
                        else
                        {
                            _LastCheckedVersion = new Version(0, 0, 0);
                        }
                    }
                    catch
                    {
                        _LastCheckedVersion = new Version(0, 0, 0);
                    }
                }

                return _LastCheckedVersion;
            }
            private set
            {
                _LastCheckedVersion = value;
                ApplicationData.Current.LocalSettings.Values["update_notified_version"] = _LastCheckedVersion.ToString();
            }
        }

        public static void UpdateLastCheckedVersionInCurrentVersion()
        {
            var currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            var currentAppVersion = new Version(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);
            LastCheckedVersion = currentAppVersion;
        }



        private static async Task<StorageFile> GetUpdateNoticesFileAsync()
        {
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var assetsFolder = await InstallationFolder.GetFolderAsync(@"Assets").AsTask();
            return await assetsFolder.GetFileAsync(@"_UpdateNotice.md");
        }



        public static async Task<string> GetUpdateNoticeAsync()
        {
            var file = await GetUpdateNoticesFileAsync();
            return await FileIO.ReadTextAsync(file);
        }

        public static async Task<bool> ShowReleaseNotePageOnBrowserAsync()
        {
            return await Launcher.LaunchUriAsync(new Uri("https://github.com/tor4kichi/Hohoema/releases"));
        }


    }
}

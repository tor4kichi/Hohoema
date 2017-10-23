using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    public static class AppUpdateNotice
    {
        // TODO: 未チェックの更新ファイルの取得
        // TODO: チェック済み更新の保存と取得

        // 更新ファイルは 00-00-00.mdの形式で保存する
        // チェックしたバージョンをApplicationDataのLocalSettings.

#if DEBUG
        private static bool __ForceNoticeUpdate = false;
#endif

        private static List<Version> _UpdateNoticeAvairableVersions;

        private static readonly AsyncLock _LoadLock = new AsyncLock();

        
        
        public static async Task<string> GetUpdateNotices(List<Version> versions, string joinString = "\r\n\r\n\r\n*****\r\n\r\n\r\n")
        {
            var versionMarkdownTextList = new List<string>();

            foreach (var version in versions)
            {
                var updateNoticeText = await GetUpdateNoticeAsync(version);
                versionMarkdownTextList.Add(updateNoticeText);
            }

            var unreadUpdateNoticeVersionsText = string.Join(joinString, versionMarkdownTextList);

            return unreadUpdateNoticeVersionsText;
        }


        public static bool HasNotCheckedUptedeNoticeVersion
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

                        _LastCheckedVersion = Version.Parse(versionText);
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

        public static async Task<List<Version>> GetNotCheckedUptedeNoticeVersions()
        {
            var currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            var currentAppVersion = new Version(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);



            // 未読の更新情報テキストを新しいものを先頭に全て表示する
            // 実際のView側での表示処理は
#if DEBUG
            if (__ForceNoticeUpdate)
            {
                var noticeableVersions = await GetUpdateNoticeAvairableVersionsAsync();
                return noticeableVersions.Take(5).ToList();
            }
#endif

            if (currentAppVersion > LastCheckedVersion)
            {
                var noticeableVersions = await GetUpdateNoticeAvairableVersionsAsync();

                var noticeVersions = noticeableVersions
                    .Where(x => x > LastCheckedVersion)
                    .ToList();


                return noticeVersions;
            }
            else
            {
                return new List<Version>();
            }
        }


        private static async Task<StorageFolder> GetUpdateNoticesFolderAsync()
        {
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var assetsFolder = await InstallationFolder.GetFolderAsync(@"Assets").AsTask();
            return await assetsFolder.GetFolderAsync(@"UpdateNotices");
        }

        public static async Task<IReadOnlyList<Version>> GetUpdateNoticeAvairableVersionsAsync()
        {
            using (var releaser = await _LoadLock.LockAsync())
            {
                if (_UpdateNoticeAvairableVersions == null)
                {
                    var list = new List<Version>();
                    var folder = await GetUpdateNoticesFolderAsync();
                    var files = await folder.GetFilesAsync();
                    foreach (var file in files)
                    {
                        var file_version = Path.GetFileNameWithoutExtension(file.Name);
                        if (Version.TryParse(file_version, out var result))
                        {
                            list.Add(result);
                        }
                    }

                    _UpdateNoticeAvairableVersions = list.OrderByDescending(x => x).ToList();
                }

                return _UpdateNoticeAvairableVersions;
            }
        }


        public static async Task<string> GetUpdateNoticeAsync(Version version)
        {
            var folder = await GetUpdateNoticesFolderAsync();
            try
            {
                var file = await folder.GetFileAsync($"{version.Major}.{version.Minor}.{version.Build}.md");
                return await FileIO.ReadTextAsync(file);
            }
            catch
            {
                return null;
            }
        }



    }
}

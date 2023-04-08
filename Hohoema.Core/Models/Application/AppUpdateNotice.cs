#nullable enable
using System;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.Storage;
using Windows.System;

namespace Hohoema.Models.Application;

public static class AppUpdateNotice
{
    // TODO: 未チェックの更新ファイルの取得
    // TODO: チェック済み更新の保存と取得

    // 更新ファイルは 00-00-00.mdの形式で保存する
    // チェックしたバージョンをApplicationDataのLocalSettings.

#if DEBUG
    private static readonly bool __ForceNoticeIsMinorVersionUpdated = false;
    private static readonly bool __ForceNoticeUpdate = true;
#endif

    public static bool IsMinorVersionUpdated
    {
        get
        {
#if DEBUG
            if (__ForceNoticeIsMinorVersionUpdated) { return true; }
#endif
            Windows.ApplicationModel.PackageVersion currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            Version currentAppVersion = new(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);
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
            Windows.ApplicationModel.PackageVersion currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            Version currentAppVersion = new(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);
            return currentAppVersion > LastCheckedVersion;
        }
    }

    private static Version _LastCheckedVersion;
    public static Version LastCheckedVersion
    {
        get
        {
            if (_LastCheckedVersion == null)
            {
                try
                {
                    string versionText = (string)ApplicationData.Current.LocalSettings.Values["update_notified_version"];

                    _LastCheckedVersion = !string.IsNullOrEmpty(versionText) ? Version.Parse(versionText) : new Version(0, 0, 0);
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
        Windows.ApplicationModel.PackageVersion currentPackegeVersion = Windows.ApplicationModel.Package.Current.Id.Version;
        Version currentAppVersion = new(currentPackegeVersion.Major, currentPackegeVersion.Minor, currentPackegeVersion.Build);
        LastCheckedVersion = currentAppVersion;
    }



    private static async Task<StorageFile> GetUpdateNoticesFileAsync()
    {
        StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        StorageFolder assetsFolder = await InstallationFolder.GetFolderAsync(@"Assets").AsTask();
        return await assetsFolder.GetFileAsync(@"_UpdateNotice.md");
    }



    public static async Task<string> GetUpdateNoticeAsync()
    {
        StorageFile file = await GetUpdateNoticesFileAsync();
        return await FileIO.ReadTextAsync(file);
    }

    public static async Task<bool> ShowReleaseNotePageOnBrowserAsync()
    {
        StoreProductResult lisence = await StoreContext.GetDefault().GetStoreProductForCurrentAppAsync();
        if (lisence.Product == null) { return false; }

        _ = await StoreContext.GetDefault().GetAppLicenseAsync();
        _ = await StoreContext.GetDefault().GetAppAndOptionalStorePackageUpdatesAsync();

        return await Launcher.LaunchUriAsync(lisence.Product.LinkUri);
    }


}

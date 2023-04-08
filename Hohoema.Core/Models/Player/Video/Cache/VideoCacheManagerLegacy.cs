using Hohoema.Models.Niconico.Video;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.Player.Video.Cache;

public struct CacheSaveFolderChangedEventArgs
{
    public StorageFolder OldFolder { get; set; }
    public StorageFolder NewFolder { get; set; }
}

public struct CacheRequestRejectedEventArgs
{
    public string Reason { get; set; }
    public CacheRequest Request { get; set; }
}


[Obsolete]
public class CacheSaveFolder
{
    public CacheSaveFolder(VideoCacheSettings_Legacy cacheSettings)
    {
        CacheSettings = cacheSettings;
    }

    /// <summary>
    /// 動画キャッシュ保存先フォルダをチェックします
    /// 選択済みだがフォルダが見つからない場合に、トースト通知を行います。
    /// </summary>
    /// <returns></returns>
    /*
		public async Task CheckVideoCacheFolderState()
    {
        var cacheFolderState = await GetVideoCacheFolderState();

        if (cacheFolderState == CacheFolderAccessState.SelectedButNotExist)
        {
            var toastService = Container.Resolve<Services.NotificationService>();
            toastService.ShowToast(
                "キャッシュが利用できません"
                , "キャッシュ保存先フォルダが見つかりません。（ここをタップで設定画面を表示）"
                , duration: Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                , toastActivatedAction: async () =>
                {
                    await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var pm = Container.Resolve<PageManager>();
                        pm.OpenPage(HohoemaPageType.CacheManagement);
                    });
                });
        }
    }
    */

    private static StorageFolder _DownloadFolder;
    private const string FolderAccessToken = "HohoemaVideoCache";

    // 旧バージョンで指定されたフォルダーでも動くようにするためにFolderAccessTokenを動的に扱う
    // 0.4.0以降はFolderAccessTokenで指定したトークンだが、
    // それ以前では ログインユーザーIDをトークンとして DL/Hohoema/ログインユーザーIDフォルダ/ をDLフォルダとして指定していた
    private static string CurrentFolderAccessToken = null;

    public string PrevCacheFolderAccessToken { get; private set; }
    public VideoCacheSettings_Legacy CacheSettings { get; }


    public event EventHandler<CacheSaveFolderChangedEventArgs> SaveFolderChanged;

    private static async Task<StorageFolder> GetEnsureVideoFolder()
    {
        if (_DownloadFolder == null)
        {
            try
            {
                // 既にフォルダを指定済みの場合
                if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderAccessToken))
                {
                    _DownloadFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderAccessToken);
                    CurrentFolderAccessToken = FolderAccessToken;
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                //					Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(FolderAccessToken);
                Debug.WriteLine(ex.ToString());
            }
        }

        return _DownloadFolder;
    }


    public async Task<bool> CanReadAccessVideoCacheFolder()
    {
        CacheFolderAccessState status = await GetVideoCacheFolderState();
        return status is CacheFolderAccessState.Exist or CacheFolderAccessState.NotEnabled;
    }

    public async Task<bool> CanWriteAccessVideoCacheFolder()
    {
        CacheFolderAccessState status = await GetVideoCacheFolderState();
        return status == CacheFolderAccessState.Exist;
    }

    public async Task<CacheFolderAccessState> GetVideoCacheFolderState()
    {
        if (false == CacheSettings.IsUserAcceptedCache)
        {
            return CacheFolderAccessState.NotAccepted;
        }

        try
        {
            StorageFolder videoFolder = await GetEnsureVideoFolder();

            if (videoFolder == null)
            {
                return CacheFolderAccessState.NotSelected;
            }
        }
        catch (FileNotFoundException)
        {
            return CacheFolderAccessState.SelectedButNotExist;
        }

        return false == CacheSettings.IsEnableCache ? CacheFolderAccessState.NotEnabled : CacheFolderAccessState.Exist;
    }



    public async Task<StorageFolder> GetVideoCacheFolder()
    {
        try
        {
            return await GetEnsureVideoFolder();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }


    public async Task<bool> ChangeUserDataFolder()
    {
        StorageFolder oldSaveFolder = _DownloadFolder;
        try
        {
            Windows.Storage.Pickers.FolderPicker folderPicker = new()
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads
            };
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                if (false == string.IsNullOrWhiteSpace(CurrentFolderAccessToken))
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.Remove(CurrentFolderAccessToken);
                    CurrentFolderAccessToken = null;
                }

                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace(FolderAccessToken, folder);

                _DownloadFolder = folder;

                CurrentFolderAccessToken = FolderAccessToken;
            }
            else
            {
                return false;
            }
        }
        catch
        {

        }
        finally
        {

        }

        SaveFolderChanged?.Invoke(this, new CacheSaveFolderChangedEventArgs()
        {
            OldFolder = oldSaveFolder,
            NewFolder = _DownloadFolder
        });

        return true;
    }

}

public struct VideoCacheStateChangedEventArgs
{
    public CacheRequest Request { get; set; }
    public NicoVideoCacheState PreviousCacheState { get; set; }
}





public enum CacheManagerState
{
    NotInitialize,
    Running,
    SuspendDownload,
}

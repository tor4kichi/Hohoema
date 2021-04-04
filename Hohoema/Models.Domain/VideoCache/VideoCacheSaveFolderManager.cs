using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.Domain.VideoCache
{
    public struct CacheSaveFolderChangedEventArgs
    {
        public StorageFolder OldFolder { get; set; }
        public StorageFolder NewFolder { get; set; }
    }

    internal class VideoCacheSaveFolderManager
    {
        internal VideoCacheSaveFolderManager()
        {
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

        StorageFolder _DownloadFolder;

        const string FolderAccessToken = "HohoemaVideoCache";

        public string PrevCacheFolderAccessToken { get; private set; }

        public event EventHandler<CacheSaveFolderChangedEventArgs> SaveFolderChanged;

        static private async ValueTask<StorageFolder> GetUserChoiceVideoFolder()
        {
            try
            {
                // 既にフォルダを指定済みの場合
                if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderAccessToken))
                {
                    return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderAccessToken);
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

            return null;
        }

        public async ValueTask<StorageFolder> GetVideoCacheFolder()
        {
            if (_DownloadFolder is not null) { return _DownloadFolder; }

            StorageFolder folder = null;
            try
            {
                folder = await GetUserChoiceVideoFolder();
            }
            catch (FileNotFoundException)
            {
                
            }

            if (folder is null)
            {
                return await DownloadsFolder.CreateFolderAsync(FolderAccessToken, CreationCollisionOption.OpenIfExists);
            }

            return _DownloadFolder = folder;
        }

        public async Task<bool> ChangeUserDataFolder()
        {
            var oldSaveFolder = _DownloadFolder;
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(FolderAccessToken);

                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(FolderAccessToken, folder);

                    _DownloadFolder = folder;
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
}

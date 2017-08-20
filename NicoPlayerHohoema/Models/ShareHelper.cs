using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Windows.ApplicationModel.DataTransfer;

namespace NicoPlayerHohoema.Models
{
    public static class ShareHelper
    {
        public static async Task ShareToTwitter(NicoVideo video)
        {
            if (!TwitterHelper.IsLoggedIn)
            {

                if (!await TwitterHelper.LoginOrRefreshToken())
                {
                    return;
                }
            }

            if (TwitterHelper.IsLoggedIn)
            {
                var textInputDialogService = App.Current.Container.Resolve<Views.Service.TextInputDialogService>();

                var text = $"{video.Title} http://nico.ms/{video.VideoId} #{video.VideoId}";
                var twitterLoginUserName = TwitterHelper.TwitterUser.ScreenName;
                var customText = await textInputDialogService.GetTextAsync($"{twitterLoginUserName} としてTwitterへ投稿", "", text);

                if (customText != null)
                {
                    var result = await TwitterHelper.SubmitTweet(customText);

                    if (!result)
                    {
                        var toastService = App.Current.Container.Resolve<Views.Service.ToastNotificationService>();
                        toastService.ShowText("ツイートに失敗しました", "もう一度お試しください");
                    }
                }
            }
        }

        

        static string _ShareText;
        
        public static void Share(NicoVideo video)
        {
            if (DataTransferManager.IsSupported())
            {
                var videoUrl = $"http://nico.ms/{video.VideoId}";
                _ShareText = $"{video.Title} {videoUrl} #{video.VideoId}";

                var dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;

                DataTransferManager.ShowShareUI();
            }
        }

        private static void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;

            request.Data.SetText(_ShareText);

            request.Data.Properties.Title = "ニコニコ動画の共有";

            sender.DataRequested -= DataTransferManager_DataRequested;
        }





        public static void CopyToClipboard(NicoVideo video)
        {
            var videoUrl = $"http://nico.ms/{video.VideoId}";
            var text = $"{video.Title} {videoUrl} #{video.VideoId}";
            var datapackage = new DataPackage();
            datapackage.SetText(text);

            Clipboard.SetContent(datapackage);
        }
    }
}

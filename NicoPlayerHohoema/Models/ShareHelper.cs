using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Windows.ApplicationModel.DataTransfer;
using NicoPlayerHohoema.Models.Live;

namespace NicoPlayerHohoema.Models
{
    public static class ShareHelper
    {
        public static string MakeShareText(NicoVideo video)
        {
            return $"{video.Title} http://nico.ms/{video.VideoId} #{video.VideoId}";
        }

        public static string MakeShareText(NicoLiveVideo live)
        {
            return MakeLiveShareText(live.LiveTitle, live.LiveId);
        }
        public static string MakeLiveShareText(string liveTitle, string liveId)
        {
            return $"【ニコ生】{liveTitle} http://nico.ms/{liveId} #{liveId}";
        }


        public static async Task ShareToTwitter(string content)
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

                var twitterLoginUserName = TwitterHelper.TwitterUser.ScreenName;
                var customText = await textInputDialogService.GetTextAsync($"{twitterLoginUserName} としてTwitterへ投稿", "", content);

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

        public static async Task ShareToTwitter(NicoVideo video)
        {
            await ShareToTwitter(MakeShareText(video));
        }

        public static async Task ShareToTwitter(NicoLiveVideo video)
        {
            await ShareToTwitter(MakeShareText(video));
        }




        static string _ShareText;


        public static void Share(string content)
        {
            if (DataTransferManager.IsSupported())
            {
                _ShareText = content;

                var dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;

                DataTransferManager.ShowShareUI();
            }
        }

        public static void Share(NicoVideo video)
        {
            Share(MakeShareText(video));
        }

        public static void Share(NicoLiveVideo video)
        {
            Share(MakeShareText(video));
        }



        private static void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;

            request.Data.SetText(_ShareText);

            request.Data.Properties.Title = "ニコニコ動画の共有";

            sender.DataRequested -= DataTransferManager_DataRequested;
        }



        public static void CopyToClipboard(string content)
        {
            var datapackage = new DataPackage();
            datapackage.SetText(content);

            Clipboard.SetContent(datapackage);
        }

        public static void CopyToClipboard(NicoVideo video)
        {
            CopyToClipboard(MakeShareText(video));
        }

        public static void CopyToClipboard(NicoLiveVideo video)
        {
            CopyToClipboard(MakeShareText(video));
        }
    }
}

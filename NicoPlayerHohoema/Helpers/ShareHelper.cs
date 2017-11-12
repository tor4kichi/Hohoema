using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Windows.ApplicationModel.DataTransfer;
using NicoPlayerHohoema.Models.Live;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;

namespace NicoPlayerHohoema.Helpers
{
    public static class ShareHelper
    {
        public static string MakeShareText(Database.NicoVideo video)
        {
            if (!string.IsNullOrEmpty(video.VideoId))
            {
                return MakeShareText(video.VideoId, video.Title);
            }
            else
            {
                return MakeShareText(video.RawVideoId, video.Title);
            }
        }

        public static string MakeShareText(Interfaces.INiconicoContent parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;
                return MakeShareText(content.Id, content.Label);
            }
            else if (parameter is Interfaces.ILiveContent)
            {
                var content = parameter as Interfaces.ILiveContent;
                return MakeShareText(content.Id, content.Label, "ニコニコ生放送");
            }
            else if (parameter is Interfaces.ICommunity)
            {
                var content = parameter as Interfaces.ICommunity;
                return MakeShareText(content.Id, content.Label, "ニコニミュニティ");
            }
            else if (parameter is Interfaces.IMylist)
            {
                var content = parameter as Interfaces.IMylist;
                return MakeShareText($"mylist/{content.Id}", content.Label);
            }
            else if (parameter is Interfaces.IUser)
            {
                var content = parameter as Interfaces.IUser;
                return MakeShareText($"user/{content.Id}", content.Label);
            }
            else
            {
                return MakeShareText(parameter.Id, parameter.Label);
            }
        }

        public static string MakeShareText(string id, string title, params string[] hashTags)
        {
            var hashTagsString = string.Join(" ", hashTags.Select(x => "#" + x));
            if (hashTagsString.Any())
            {
                hashTagsString += " ";
            }
            return $"{title} http://nico.ms/{id} #{id} {hashTagsString}#Hohoema";
        }

        public static string MakeShareText(NicoLiveVideo live)
        {
            return MakeLiveShareText(live.LiveTitle, live.LiveId);
        }
        public static string MakeLiveShareText(string liveTitle, string liveId)
        {
            return $"{liveTitle} http://nico.ms/{liveId} #{liveId} #ニコニコ生放送 #Hohoema";
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
                var textInputDialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();

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

        public static async Task ShareToTwitter(Database.NicoVideo video)
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

        public static void Share(Database.NicoVideo video)
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

        public static void CopyToClipboard(Database.NicoVideo video)
        {
            CopyToClipboard(MakeShareText(video));
        }

        public static void CopyToClipboard(NicoLiveVideo video)
        {
            CopyToClipboard(MakeShareText(video));
        }
    }
}

using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Hohoema.Models.Helpers
{
    public static class ShareHelper
    {
        static public Uri ConvertToUrl(INiconicoObject content)
        {
            Uri uri = null;
            switch (content)
            {
                case IUser user:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeUserPageUrl(user.Id)));
                    break;
                case IVideoContent videoContent:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl, videoContent.Id));
                    break;
                case IMylist mylist:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeMylistPageUrl(mylist.Id)));
                    break;
                case ILiveContent live:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.LiveWatchPageUrl, live.Id));
                    break;
                case IChannel channel:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.ChannelUrlBase, "channel", "ch" + channel.Id));
                    break;
                case ICommunity community:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.CommynitySammaryPageUrl, community.Id));
                    break;

                default:
                    break;
            }

            return uri;
        }

        public static string MakeShareText(NicoVideo video)
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

        public static string MakeShareText(INiconicoContent parameter)
        {
            if (parameter is IVideoContent)
            {
                var content = parameter as IVideoContent;
                return MakeShareText(content.Id, content.Label);
            }
            else if (parameter is ILiveContent)
            {
                var content = parameter as ILiveContent;
                return MakeShareText(content.Id, content.Label, "ニコニコ生放送");
            }
            else if (parameter is ICommunity)
            {
                var content = parameter as ICommunity;
                return MakeShareText(content.Id, content.Label, "ニコニミュニティ");
            }
            else if (parameter is IMylist)
            {
                var content = parameter as IMylist;
                return MakeShareText($"mylist/{content.Id}", content.Label);
            }
            else if (parameter is IUser)
            {
                var content = parameter as IUser;
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

        public static string MakeLiveShareText(string liveTitle, string liveId)
        {
            return $"{liveTitle} http://nico.ms/{liveId} #{liveId} #ニコニコ生放送 #Hohoema";
        }


        public static async Task ShareToTwitter(string content)
        {
            /*
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
            */
            await Task.CompletedTask;
        }

        public static async Task ShareToTwitter(NicoVideo video)
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



        private static void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;

            request.Data.SetText(_ShareText);

            request.Data.Properties.Title = "　";
            request.Data.Properties.ApplicationName = "Hohoema";

            sender.DataRequested -= DataTransferManager_DataRequested;
        }



        
    }
}

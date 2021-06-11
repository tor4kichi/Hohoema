using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.Channels;
using NiconicoToolkit.Community;
using NiconicoToolkit.Live;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Hohoema.Models.Helpers
{
    public static class ShareHelper
    {
        public static Uri ConvertToUrl(INiconicoObject content)
        {
            Uri uri = null;
            switch (content)
            {
                case IUser user:
                    uri = new Uri(UserClient.MakeUserPageUrl(user.Id));
                    break;
                case IVideoContent videoContent:
                    uri = new Uri(VideoClient.MakeWatchPageUrl(videoContent.Id));
                    break;
                case IMylist mylist:
                    uri = new Uri(MylistClient.MakeMylistPageUrl(mylist.Id));
                    break;
                case ILiveContent live:
                    uri = new Uri(LiveClient.MakeLiveWatchPageUrl(live.Id));
                    break;
                case IChannel channel:
                    uri = new Uri(ChannelClient.MakeChannelPageUrl(channel.Id));
                    break;
                case ICommunity community:
                    uri = new Uri(CommunityClient.Urls.MakeCommunityPageUrl(community.Id));
                    break;

                default:
                    break;
            }

            return uri;
        }

        public static string MakeShareTextWithTitle(NicoVideo video)
        {
            return $"{video.Title} {MakeShareText(video)}";
        }

        public static string MakeShareText(NicoVideo video)
        {
            if (!string.IsNullOrEmpty(video.VideoId))
            {
                return MakeShareText(video.VideoId);
            }
            else
            {
                return MakeShareText(video.RawVideoId);
            }
        }

        public static string MakeShareText(INiconicoContent parameter)
        {
            if (parameter is IVideoContent videoContent)
            {
                return MakeShareText(videoContent.Id);
            }
            else if (parameter is ILiveContent liveContent)
            {
                return MakeShareText(liveContent.Id, "ニコニコ生放送");
            }
            else if (parameter is ICommunity communityContent)
            {
                return MakeShareText(communityContent.Id, "ニコニミュニティ");
            }
            else if (parameter is IMylist mylistContent)
            {
                return MakeShareText($"mylist/{mylistContent.Id}");
            }
            else if (parameter is IUser userContent)
            {
                return MakeShareText($"user/{userContent.Id}");
            }
            else
            {
                return MakeShareText(parameter.Id);
            }
        }

        public static string MakeShareTextWithTitle(INiconicoContent parameter)
        {
            return $"{parameter.Label} {MakeShareText(parameter)}";
        }

        public static string MakeShareText(string id, params string[] hashTags)
        {
            var hashTagsString = string.Join(" ", hashTags.Select(x => "#" + x));
            if (hashTagsString.Any())
            {
                hashTagsString += " ";
            }
            return $"http://nico.ms/{id} #{id} {hashTagsString}#Hohoema";
        }

        public static string MakeLiveShareText(string liveId)
        {
            return MakeShareText(liveId, "ニコニコ生放送");
        }

        public static string MakeLiveShareTextWithTitle(string title, string liveId)
        {
            return $"{title} {MakeShareText(liveId, "ニコニコ生放送")}";
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
            await ShareToTwitter(MakeShareTextWithTitle(video));
        }




        static string _ShareText;
        static string _ShareTitleText;

        public static void Share(string title, string content)
        {
            if (DataTransferManager.IsSupported())
            {
                _ShareText = content;
                _ShareTitleText = title;
                var dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;

                DataTransferManager.ShowShareUI();
            }
        }

        public static void Share(INiconicoContent content)
        {
            Share(content.Label, MakeShareText(content));
        }



        private static void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;

            request.Data.SetText(_ShareText);

            request.Data.Properties.Title = _ShareTitleText;
            request.Data.Properties.ApplicationName = "Hohoema";

            sender.DataRequested -= DataTransferManager_DataRequested;
        }



        
    }
}

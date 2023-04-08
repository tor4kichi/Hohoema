#nullable enable
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Hohoema.Helpers;

public static class ShareHelper
{
    public static Uri ConvertToUrl(INiconicoObject content)
    {
        Uri uri = null;
        switch (content)
        {
            case IUser user:
                uri = new Uri(NiconicoUrls.MakeUserPageUrl(user.UserId));
                break;
            case IVideoContent videoContent:
                uri = new Uri(NiconicoUrls.MakeWatchPageUrl(videoContent.VideoId));
                break;
            case IMylist mylist:
                uri = new Uri(NiconicoUrls.MakeMylistPageUrl(mylist.MylistId));
                break;
            case ILiveContent live:
                uri = new Uri(NiconicoUrls.MakeLiveWatchPageUrl(live.LiveId));
                break;
            case IChannel channel:
                uri = new Uri(NiconicoUrls.MakeChannelPageUrl(channel.ChannelId));
                break;
            case ICommunity community:
                uri = new Uri(NiconicoUrls.MakeCommunityPageUrl(community.CommunityId));
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
        return !string.IsNullOrEmpty(video.VideoAliasId) ? MakeShareText(video.VideoAliasId) : MakeShareText(video.Id);
    }

    public static string MakeShareText(INiconicoObject parameter)
    {
        if (parameter is IVideoContent videoContent)
        {
            return MakeShareText(videoContent.VideoId);
        }
        else if (parameter is ILiveContent liveContent)
        {
            return MakeShareText(liveContent.LiveId, "ニコニコ生放送");
        }
        else if (parameter is ICommunity communityContent)
        {
            return MakeShareText(communityContent.CommunityId, "ニコニミュニティ");
        }
        else
        {
            return parameter is IMylist mylistContent
                ? MakeShareText($"mylist/{mylistContent.MylistId}")
                : parameter is IUser userContent ? MakeShareText($"user/{userContent.UserId}") : throw new NotSupportedException();
        }
    }

    public static string MakeShareTextWithTitle(INiconicoObject parameter)
    {
        return $"{parameter.GetLabel()} {MakeShareText(parameter)}";
    }

    public static string MakeShareText(string id, params string[] hashTags)
    {
        string hashTagsString = string.Join(" ", hashTags.Select(x => "#" + x));
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

    private static string _ShareText;
    private static string _ShareTitleText;

    public static void Share(string title, string content)
    {
        if (DataTransferManager.IsSupported())
        {
            _ShareText = content;
            _ShareTitleText = title;
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;

            DataTransferManager.ShowShareUI();
        }
    }

    public static void Share(INiconicoObject content)
    {
        Share(content.GetLabel(), MakeShareText(content));
    }



    private static void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        DataRequest request = args.Request;

        request.Data.SetText(_ShareText);

        request.Data.Properties.Title = _ShareTitleText;
        request.Data.Properties.ApplicationName = "Hohoema";

        sender.DataRequested -= DataTransferManager_DataRequested;
    }
}

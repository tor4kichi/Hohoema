#nullable enable
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Hohoema.Models.Application;

public static class ToastNotificationConstants
{
    public const string ToastArgumentKey_Id = "id";
    public const string ToastArgumentKey_Action = "action";
    public const string ToastArgumentKey_PlaylistId = "playlistId";
    public const string ToastArgumentKey_PlaylistOrigin = "playlistOrigin";
    public const string ToastArgumentKey_PageType = "pageType";
    public const string ToastArgumentKey_PageParameters = "pageParameters";
    public const string ToastArgumentValue_Action_PlayVideo = "playVideo";
    public const string ToastArgumentValue_Action_PlayPlaylist = "playPlaylist";
    public const string ToastArgumentValue_Action_OpenPage = "openPage";
    public const string ToastArgumentValue_Action_DeleteCache = "delete";

    public const string ProgressBarBindableValueKey_ProgressValue = "progressValue";
    public const string ProgressBarBindableValueKey_ProgressValueOverrideString = "progressValueString";
    public const string ProgressBarBindableValueKey_ProgressStatus = "progressStatus";



    public static ToastArguments MakePlayVideoToastArguments(string videoId)
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_PlayVideo },
            { ToastArgumentKey_Id, videoId }
        };
        return args;
    }

    public static ToastArguments MakePlayVideoToastArguments(string videoId, string playlistId)
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_PlayVideo },
            { ToastArgumentKey_Id, videoId },
            { ToastArgumentKey_PlaylistId, playlistId }
        };
        return args;
    }

    public static ToastArguments MakePlayPlaylistToastArguments(PlaylistItemsSourceOrigin origin, string playlistId)
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_PlayPlaylist },
            { ToastArgumentKey_PlaylistOrigin, origin.ToString() },
            { ToastArgumentKey_PlaylistId, playlistId }
        };
        return args;
    }

    public static ToastArguments MakeOpenPageToastArguments(HohoemaPageType pageType, string parameters = null)
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_OpenPage },
            { ToastArgumentKey_PageType, pageType.ToString() }
        };
        if (parameters != null)
        {
            _ = args.Add(ToastArgumentKey_PageParameters, parameters);
        }
        return args;
    }

    public static ToastArguments MakeOpenPageWithIdToastArguments(HohoemaPageType pageType, string id)
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_OpenPage },
            { ToastArgumentKey_PageType, pageType.ToString() },
            { ToastArgumentKey_PageParameters, $"id={id}" }
        };
        return args;
    }


    public static ToastArguments MakeDeleteCacheToastArguments(string videoId)
    {
        ToastArguments args = new()
        {
            { ToastArgumentKey_Action, ToastArgumentValue_Action_DeleteCache },
            { ToastArgumentKey_Id, videoId }
        };
        return args;
    }
}

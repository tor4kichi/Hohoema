using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Application
{
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
            var args = new ToastArguments();
            args.Add(ToastArgumentKey_Action, ToastArgumentValue_Action_PlayVideo);
            args.Add(ToastArgumentKey_Id, videoId);
            return args;
        }

        public static ToastArguments MakePlayVideoToastArguments(string videoId, string playlistId)
        {
            var args = new ToastArguments();
            args.Add(ToastArgumentKey_Action, ToastArgumentValue_Action_PlayVideo);
            args.Add(ToastArgumentKey_Id, videoId);
            args.Add(ToastArgumentKey_PlaylistId, playlistId);
            return args;
        }

        public static ToastArguments MakePlayPlaylistToastArguments(PlaylistItemsSourceOrigin origin, string playlistId)
        {
            var args = new ToastArguments();
            args.Add(ToastArgumentKey_Action, ToastArgumentValue_Action_PlayPlaylist);
            args.Add(ToastArgumentKey_PlaylistOrigin, origin.ToString());
            args.Add(ToastArgumentKey_PlaylistId, playlistId);
            return args;
        }

        public static ToastArguments MakeOpenPageToastArguments(HohoemaPageType pageType, string parameters = null)
        {
            var args = new ToastArguments();
            args.Add(ToastArgumentKey_Action, ToastArgumentValue_Action_OpenPage);
            args.Add(ToastArgumentKey_PageType, pageType.ToString());
            if (parameters != null)
            {
                args.Add(ToastArgumentKey_PageParameters, parameters);
            }
            return args;
        }

        public static ToastArguments MakeOpenPageWithIdToastArguments(HohoemaPageType pageType, string id)
        {
            var args = new ToastArguments();
            args.Add(ToastArgumentKey_Action, ToastArgumentValue_Action_OpenPage);
            args.Add(ToastArgumentKey_PageType, pageType.ToString());
            args.Add(ToastArgumentKey_PageParameters, $"id={id}");
            return args;
        }


        public static ToastArguments MakeDeleteCacheToastArguments(string videoId)
        {
            var args = new ToastArguments();
            args.Add(ToastArgumentKey_Action, ToastArgumentValue_Action_DeleteCache);
            args.Add(ToastArgumentKey_Id, videoId);
            return args;
        }
    }
}

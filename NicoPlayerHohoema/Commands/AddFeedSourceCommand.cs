using Mntone.Nico2;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
   
    public sealed class AddFeedSourceCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            if (parameter is Database.Bookmark)
            {
                return true;
            }

            if (parameter is Models.FollowItemInfo)
            {
                // コミュニティは動画リストをうまく扱えないため対応しない
                return (parameter as Models.FollowItemInfo).FollowItemType != Models.FollowItemType.Community;
            }

            if (parameter is Interfaces.IVideoContent)
            {
                return true;
            }

            return false;
        }

        protected override async void Execute(object parameter)
        {
            Database.Bookmark bookmark = null;

            if (parameter is Database.Bookmark)
            {
                bookmark = parameter as Database.Bookmark;
            }
            else if (parameter is Models.FollowItemInfo)
            {
                var followInfo = parameter as Models.FollowItemInfo;
                bookmark = new Database.Bookmark()
                {
                    Label = followInfo.Name,
                    BookmarkType = Models.FeedManager.FollowItemTypeConvertToFeedSourceType(followInfo.FollowItemType),
                    Content = followInfo.Id
                };
            }
            else if (parameter is Interfaces.IVideoContent)
            {
                var videoInfo = parameter as Interfaces.IVideoContent;

                if (videoInfo.OwnerUserId == null)
                {
                    System.Diagnostics.Debug.WriteLine("フィード登録失敗、動画投稿者の情報がありません。");
                    return;
                }
                var hohoemaApp = App.Current.Container.Resolve<Models.HohoemaApp>();

                var userInfo = await hohoemaApp.ContentProvider.GetUserInfo(videoInfo.OwnerUserId);
                if (userInfo != null)
                {
                    bookmark = new Database.Bookmark()
                    {
                        Label = userInfo.Nickname,
                        BookmarkType = Database.BookmarkType.User,
                        Content = videoInfo.OwnerUserId
                    };
                }
            }

            if (bookmark != null)
            {
                var hohoemaApp = App.Current.Container.Resolve<Models.HohoemaApp>();

                if (hohoemaApp.FeedManager.GetAllFeedGroup()
                        .Where(x => x.Sources.Any())
                        .Select(x => x.Sources.First())
                        .Any(x => x.BookmarkType == bookmark.BookmarkType && x.Content == bookmark.Content))
                {
                    return;
                }

                var feedGroup = hohoemaApp.FeedManager.AddFeedGroup(bookmark.Label, bookmark);

                if (feedGroup != null)
                {
                    // 通知
                    var notificationService = HohoemaCommnadHelper.GetNotificationService();
                    notificationService.ShowInAppNotification(
                        Services.InAppNotificationPayload.CreateReadOnlyNotification(
                            $"{feedGroup.Label} を新着チェック対象として追加しました"
                            )
                        );
                        
                }
            }
        }
    }
}

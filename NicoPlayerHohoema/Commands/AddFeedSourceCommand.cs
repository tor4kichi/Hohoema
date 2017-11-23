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
            if (parameter is Database.Bookmark)
            {
                var hohoemaApp = App.Current.Container.Resolve<Models.HohoemaApp>();
                var bookmark = parameter as Database.Bookmark;
                var targetTitle = bookmark.Label;
                var feedGroup = await hohoemaApp.ChoiceFeedGroup(targetTitle + "をフィードに追加");
                if (feedGroup != null)
                {
                    var result = feedGroup.AddSource(bookmark);

                    // 通知
                    var registrationResult = result ? ContentManageResult.Success : ContentManageResult.Failed;
                    (App.Current as App).PublishInAppNotification(
                        Models.InAppNotificationPayload.CreateRegistrationResultNotification(
                            registrationResult,
                            "フィード",
                            feedGroup.Label,
                            targetTitle
                            ));
                }
            }
            else if (parameter is Models.FollowItemInfo)
            {
                var hohoemaApp = App.Current.Container.Resolve<Models.HohoemaApp>();
                var followInfo = parameter as Models.FollowItemInfo;
                var bookmark = new Database.Bookmark()
                {
                    Label = followInfo.Name,
                    BookmarkType = Models.FeedManager.FollowItemTypeConvertToFeedSourceType(followInfo.FollowItemType),
                    Content = followInfo.Id
                };
                var targetTitle = bookmark.Label;
                var feedGroup = await hohoemaApp.ChoiceFeedGroup(targetTitle + "をフィードに追加");
                if (feedGroup != null)
                {
                    var result = feedGroup.AddSource(bookmark);

                    // 通知
                    var registrationResult = result ? ContentManageResult.Success : ContentManageResult.Failed;
                    (App.Current as App).PublishInAppNotification(
                        Models.InAppNotificationPayload.CreateRegistrationResultNotification(
                            registrationResult,
                            "フィード",
                            feedGroup.Label,
                            targetTitle
                            ));
                }
            }
            else if (parameter is Interfaces.IVideoContent)
            {
                
                var hohoemaApp = App.Current.Container.Resolve<Models.HohoemaApp>();
                var videoInfo = parameter as Interfaces.IVideoContent;

                if (videoInfo.OwnerUserId == null || videoInfo.OwnerUserName == null)
                {
                    System.Diagnostics.Debug.WriteLine("フィード登録失敗、動画投稿者の情報がありません。");
                    return;
                }
                var bookmark = new Database.Bookmark()
                {
                    Label = videoInfo.OwnerUserName,
                    BookmarkType = Database.BookmarkType.User,
                    Content = videoInfo.OwnerUserId
                };
                var targetTitle = bookmark.Label;
                var feedGroup = await hohoemaApp.ChoiceFeedGroup(targetTitle + "をフィードに追加");
                if (feedGroup != null)
                {
                    var result = feedGroup.AddSource(bookmark);

                    // 通知
                    var registrationResult = result ? ContentManageResult.Success : ContentManageResult.Failed;
                    (App.Current as App).PublishInAppNotification(
                        Models.InAppNotificationPayload.CreateRegistrationResultNotification(
                            registrationResult,
                            "フィード",
                            feedGroup.Label,
                            targetTitle
                            ));
                }
            }
        }
    }
}

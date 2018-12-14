using NicoPlayerHohoema.Helpers;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class ChoiceSubscriptionSourceCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.Subscription;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Models.Subscription.Subscription subscription)
            {
                var hohoemaApp = Commands.HohoemaCommnadHelper.GetHohoemaApp();
                var dialogService = Commands.HohoemaCommnadHelper.GetDialogService();

                // フォローしているアイテムから選択できるように
                // （コミュニティを除く）
                var selectableContents = hohoemaApp.FollowManager.GetAllFollowInfoGroups()
                            .Select(x => new Dialogs.ChoiceFromListSelectableContainer(x.FollowItemType.ToCulturelizeString(),
                                x.FollowInfoItems.Where(y => y.FollowItemType != Models.FollowItemType.Community).Select(y => new Dialogs.SelectDialogPayload()
                                {
                                    Label = y.Name,
                                    Id = y.Id,
                                    Context = y
                                })));

                var keywordInput = new Dialogs.TextInputSelectableContainer("キーワード検索", null);

                var result = await dialogService.ShowContentSelectDialogAsync($"『{subscription.Label}』へ購読を追加", Enumerable.Concat<Dialogs.ISelectableContainer>(new[] { keywordInput }, selectableContents));

                if (result != null)
                {
                    Models.Subscription.SubscriptionSource? source = null;
                    if (result.Context is Models.FollowItemInfo followInfo)
                    {
                        Models.Subscription.SubscriptionSourceType? sourceType = null;
                        switch (followInfo.FollowItemType)
                        {
                            case Models.FollowItemType.Tag:
                                sourceType = Models.Subscription.SubscriptionSourceType.TagSearch;
                                break;
                            case Models.FollowItemType.Mylist:
                                sourceType = Models.Subscription.SubscriptionSourceType.Mylist;
                                break;
                            case Models.FollowItemType.User:
                                sourceType = Models.Subscription.SubscriptionSourceType.User;
                                break;
                            case Models.FollowItemType.Community:
                                break;
                            case Models.FollowItemType.Channel:
                                sourceType = Models.Subscription.SubscriptionSourceType.Channel;
                                break;
                            default:
                                break;
                        }

                        if (sourceType != null)
                        {
                            source = new Models.Subscription.SubscriptionSource(result.Label, sourceType.Value, result.Id);
                        }
                    }
                    else
                    {
                        source = new Models.Subscription.SubscriptionSource(result.Label, Models.Subscription.SubscriptionSourceType.KeywordSearch, result.Id);
                    }

                    if (subscription.AddSource.CanExecute(source))
                    {
                        subscription.AddSource.Execute(source);
                    }
                }
            }
        }
    }
}

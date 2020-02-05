using I18NPortable;
using NicoPlayerHohoema.Dialogs;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Helpers;
using Prism.Commands;
using System.Linq;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class ChoiceSubscriptionSourceCommand : DelegateCommandBase
    {
        public ChoiceSubscriptionSourceCommand(
            FollowManager followManager,
            DialogService dialogService)
        {
            FollowManager = followManager;
            DialogService = dialogService;
        }

        public FollowManager FollowManager { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.Subscription;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Models.Subscription.Subscription subscription)
            {
                // フォローしているアイテムから選択できるように
                // （コミュニティを除く）
                var selectableContents = FollowManager.GetAllFollowInfoGroups()
                            .Select(x => new ChoiceFromListSelectableContainer(x.FollowItemType.Translate(),
                                x.FollowInfoItems.Where(y => y.FollowItemType != Models.FollowItemType.Community).Select(y => new SelectDialogPayload()
                                {
                                    Label = y.Name,
                                    Id = y.Id,
                                    Context = y
                                })));

                var keywordInput = new TextInputSelectableContainer("キーワード検索", null);

                var result = await DialogService.ShowContentSelectDialogAsync(
                    $"『{subscription.Label}』へ購読を追加", 
                    Enumerable.Concat<ISelectableContainer>(new[] { keywordInput }, selectableContents)
                    );

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

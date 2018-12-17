using NicoPlayerHohoema.Dialogs;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.LocalMylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Web.Http;

namespace NicoPlayerHohoema.Services
{
    public class DialogService
    {
        public DialogService(
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager
            )
        {
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
        }

        #region Cache Accept Usase Dialog

        static readonly string CacheUsageConfirmationFileUri = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\CacheUsageConfirmation.md";

        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }

        public async Task<bool> ShowAcceptCacheUsaseDialogAsync(bool showWithoutConfirmButton = false)
        {
            var dialog = new Dialogs.MarkdownTextDialog("キャッシュ機能の利用に関する確認");

            
            var file = await StorageFile.GetFileFromPathAsync(CacheUsageConfirmationFileUri);
            dialog.Text = await FileIO.ReadTextAsync(file);
            
            if (!showWithoutConfirmButton)
            {
                dialog.PrimaryButtonText = "同意する";
                dialog.SecondaryButtonText = "キャンセル";
            }
            else
            {
                dialog.PrimaryButtonText = "閉じる";
            }

            var result = await dialog.ShowAsync();

            return result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary;
        }

        #endregion



        #region Update Notice Dialog

        public async Task ShowUpdateNoticeAsync(Version version)
        {
            var allVersions = await Models.Helpers.AppUpdateNotice.GetUpdateNoticeAvairableVersionsAsync();
            var versions = allVersions.Where(x => x.Major == version.Major && x.Minor == version.Minor).ToList();
            var text = await Models.Helpers.AppUpdateNotice.GetUpdateNotices(versions);
            var dialog = new Dialogs.MarkdownTextDialog($"v{version.Major}.{version.Minor} 更新情報 一覧");
            dialog.Text = text;
            dialog.PrimaryButtonText = "OK";
            await dialog.ShowAsync();

        }

        public async Task ShowLatestUpdateNotice()
        {
            var versions = await Models.Helpers.AppUpdateNotice.GetNotCheckedUptedeNoticeVersions();

            if (versions.Count == 0) { return; }

            var text = await Models.Helpers.AppUpdateNotice.GetUpdateNotices(versions);
            var dialog = new Dialogs.MarkdownTextDialog("更新情報");
            dialog.Text = text;
            dialog.PrimaryButtonText = "OK";

            try
            {
                var addon = await Models.Purchase.HohoemaPurchase.GetAvailableCheersAddOn();
                var product = addon.ProductListings.FirstOrDefault(x => Models.Purchase.HohoemaPurchase.ProductIsActive(x.Value));

                if (product.Value != null)
                {
                    dialog.SecondaryButtonText = "開発支援について確認する";
                    dialog.SecondaryButtonClick += async (_, e) =>
                    {
                        await Models.Purchase.HohoemaPurchase.RequestPurchase(product.Value);
                    };
                }
            }
            catch { }

            await dialog.ShowAsync();
        }


        #endregion


        #region ContentSelectDialog

        public Task<SelectDialogPayload> ShowContentSelectDialogAsync(ContentSelectDialogDefaultSet dialogContentSet)
        {
            var choiceListContainer = new ChoiceFromListSelectableContainer(dialogContentSet.ChoiceListTitle, dialogContentSet.ChoiceList);
            var customTextContainer = new TextInputSelectableContainer(dialogContentSet.TextInputTitle, dialogContentSet.GenerateCandiateList);

            var containers = new List<ISelectableContainer>();

            ISelectableContainer firstSelected = null;
            if (!string.IsNullOrEmpty(dialogContentSet.ChoiceListTitle))
            {
                containers.Add(choiceListContainer);

                if (choiceListContainer.Items.Count > 0)
                {
                    firstSelected = choiceListContainer;
                }
                else
                {
                    firstSelected = choiceListContainer;
                }
            }

            if (!string.IsNullOrEmpty(dialogContentSet.TextInputTitle))
            {
                containers.Add(customTextContainer);

                if (firstSelected == null)
                {
                    firstSelected = customTextContainer;
                }
            }

            return ShowContentSelectDialogAsync(dialogContentSet.DialogTitle, containers, firstSelected);


        }

        public Task<SelectDialogPayload> ShowContentSelectDialogAsync(string dialogTitle, IEnumerable<ISelectableContainer> containers, int defaultContainerIndex = 0)
        {
            var firstSelected = containers.ElementAtOrDefault(defaultContainerIndex);
            return ShowContentSelectDialogAsync(dialogTitle, containers, firstSelected);
        }

        private async Task<SelectDialogPayload> ShowContentSelectDialogAsync(string dialogTitle, IEnumerable<ISelectableContainer> containers, ISelectableContainer firstSelected)
        {
            var context = new ContentSelectDialogContext(dialogTitle, containers, firstSelected);

            SelectDialogPayload resultContent = null;
            try
            {
                var dialog = new Dialogs.ContentSelectDialog()
                {
                    DataContext = context
                };

                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    resultContent = context.GetResult();
                }
            }
            finally
            {
                context?.Dispose();
            }

            return resultContent;
        }

        #endregion



        #region RankingChoiceDialog

        public Task<List<T>> ShowMultiChoiceDialogAsync<T, X>(
            string title,
            IEnumerable<T> selectableItems,
            IEnumerable<T> selectedItems,
            Expression<Func<T, X>> memberPathExpression
            )
        {
            return ShowMultiChoiceDialogAsync(
                title, 
                selectableItems, 
                selectedItems, 
                ((MemberExpression)memberPathExpression.Body).Member.Name
                );
        }


        public async Task<List<T>> ShowMultiChoiceDialogAsync<T>(
            string title,
            IEnumerable<T> selectableItems,
            IEnumerable<T> selectedItems,
            string memberPathName
            )
        {
            var multiChoiceDialog = new Dialogs.MultiChoiceDialog();

            multiChoiceDialog.Title = title;
            multiChoiceDialog.Items = selectableItems.ToList();
            multiChoiceDialog.SelectedItems = selectedItems.ToList();
            multiChoiceDialog.DisplayMemberPath = memberPathName;

            var result = await multiChoiceDialog.ShowAsync();

            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return multiChoiceDialog.SelectedItems.Cast<T>().ToList();
            }
            else
            {
                return null;
            }
        }

        #endregion



        #region EditMylistGroupDialog


        public Task<bool> ShowEditMylistGroupDialogAsync(MylistGroupEditData data)
        {
            return ShowMylistGroupDialogAsync(data, false);

        }
        public Task<bool> ShowCreateMylistGroupDialogAsync(MylistGroupEditData data)
        {
            return ShowMylistGroupDialogAsync(data, true);
        }

        private async Task<bool> ShowMylistGroupDialogAsync(MylistGroupEditData data, bool isCreate)
        {
            var context = new EditMylistGroupDialogContext(data, isCreate);
            var dialog = new EditMylistGroupDialog()
            {
                DataContext = context
            };

            var result = await dialog.ShowAsync();

            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                var resultData = context.GetResult();
                data.Name = resultData.Name;
                data.Description = resultData.Description;
                data.IconType = resultData.IconType;
                data.IsPublic = resultData.IsPublic;
                data.MylistDefaultSort = resultData.MylistDefaultSort;
            }
            return result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary;
        }


        #endregion


        #region GetTextDialog

        public async Task<string> GetTextAsync(string title, string placeholder, string defaultText = "", Func<string, bool> validater = null)
        {
            if (validater == null)
            {
                validater = (_) => true;
            }
            var context = new TextInputDialogContext(title, placeholder, defaultText, validater);

            var dialog = new TextInputDialog()
            {
                DataContext = context
            };

            var result = await dialog.ShowAsync();

            // 仮想入力キーボードを閉じる
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();

            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return context.GetValidText();
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region Niconico Two Factor Auth Dialog


        public async Task ShowNiconicoTwoFactorLoginDialog(object content)
        {
            var dialog = new NiconicoTwoFactorAuthDialog();

            dialog.WebViewContent = content;
            await dialog.ShowAsync();
        }

        #endregion




        public async Task<bool> ShowMessageDialog(string content, string title, string acceptButtonText = null, string cancelButtonText = null)
        {
            var dialog = new MessageDialog(content, title);
            if (acceptButtonText != null)
            {
                dialog.Commands.Add(new UICommand(acceptButtonText) { Id = "accept" });
            }

            if (cancelButtonText != null)
            {
                dialog.Commands.Add(new UICommand(cancelButtonText) { Id = "cancel" });
            }

            var result = await dialog.ShowAsync();

            return (result?.Id as string) == "accept";
        }




        #region

        public async Task<Interfaces.IUserOwnedMylist> ChoiceMylist(
            params string[] ignoreMylistId
            )
        {
            const string CreateNewContextLabel = @"@create_new";
            var mylists = UserMylistManager.UserMylists;
            var localMylists = LocalMylistManager.LocalMylistGroups;

            List<ISelectableContainer> selectDialogContent;
            if (false)
            {
                selectDialogContent = new List<ISelectableContainer>()
                {
                    new ChoiceFromListSelectableContainer("マイリスト",
                        mylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("ローカルマイリスト",
                        localMylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("新規作成",
                        new [] {
                            new SelectDialogPayload() { Label = "マイリストを作成", Id = "mylist", Context = CreateNewContextLabel},
                            new SelectDialogPayload() { Label = "ローカルマイリストを作成", Id = "local", Context = CreateNewContextLabel},
                        }
                    )
                };
            }
            else
            {
                selectDialogContent = new List<ISelectableContainer>()
                {
                    new ChoiceFromListSelectableContainer("ローカルマイリスト",
                        localMylists.Where(x => ignoreMylistId.All(y => x.Id != y))
                            .Select(x => new SelectDialogPayload() { Label = x.Label, Id = x.Id, Context = x })
                    ),
                    new ChoiceFromListSelectableContainer("新規作成",
                        new [] {
                            new SelectDialogPayload() { Label = "ローカルマイリストを作成", Id = "local", Context = CreateNewContextLabel},
                        }
                    )
                };

            }

            Interfaces.IUserOwnedMylist resultList = null;
            while (resultList == null)
            {
                var result = await ShowContentSelectDialogAsync(
                    "追加先マイリストを選択",
                    selectDialogContent
                    );

                if (result == null) { break; }

                if (result?.Context as string == CreateNewContextLabel)
                {
                    var mylistTypeLabel = result.Id == "mylist" ? "マイリスト" : "ローカルマイリスト";
                    var title = await GetTextAsync(
                        $"{mylistTypeLabel}を作成",
                        $"{mylistTypeLabel}名",
                        validater: (str) => !string.IsNullOrWhiteSpace(str)
                        );
                    if (title == null)
                    {
                        continue;
                    }

                    if (result.Id == "mylist")
                    {
                        await UserMylistManager.AddMylist(title, "", false, Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Descending, Mntone.Nico2.Mylist.IconType.Default);
                        resultList = UserMylistManager.UserMylists.FirstOrDefault(x => x.Label == title);
                    }
                    else //if (result.Id == "local")
                    {
                        var localMylist = new LocalMylistGroup(Guid.NewGuid().ToString(), title);
                        LocalMylistManager.LocalMylistGroups.Add(localMylist);
                        resultList = localMylist;
                    }
                }
                else
                {
                    resultList = result?.Context as Interfaces.IUserOwnedMylist;
                }
            }

            return resultList;
        }


        #endregion
    }
}

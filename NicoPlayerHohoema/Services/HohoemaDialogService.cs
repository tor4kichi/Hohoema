using NicoPlayerHohoema.Dialogs;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace NicoPlayerHohoema.Services
{
    public class HohoemaDialogService
    {
        #region Cache Accept Usase Dialog

        static readonly string CacheUsageConfirmationFileUri = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\CacheUsageConfirmation.md";

        public async Task<bool> ShowAcceptCacheUsaseDialogAsync(bool showWithoutConfirmButton = false)
        {
            var dialog = new Dialogs.MarkdownTextDialog("キャッシュ機能の利用に関する確認");

            
            var file = await StorageFile.GetFileFromPathAsync(CacheUsageConfirmationFileUri);
            dialog.Text = await FileIO.ReadTextAsync(file);
            
            if (!showWithoutConfirmButton)
            {
                dialog.PrimaryButtonText = "上記項目に同意してキャッシュを利用する";
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
            var allVersions = await Models.AppUpdateNotice.GetUpdateNoticeAvairableVersionsAsync();
            var versions = allVersions.Where(x => x.Major == version.Major && x.Minor == version.Minor).ToList();
            var text = await Models.AppUpdateNotice.GetUpdateNotices(versions);
            var dialog = new Dialogs.MarkdownTextDialog($"v{version.Major}.{version.Minor} 更新情報 一覧");
            dialog.Text = text;
            dialog.PrimaryButtonText = "OK";
            await dialog.ShowAsync();

        }

        public async Task ShowLatestUpdateNotice()
        {
            var versions = await Models.AppUpdateNotice.GetNotCheckedUptedeNoticeVersions();

            if (versions.Count == 0) { return; }

            var text = await Models.AppUpdateNotice.GetUpdateNotices(versions);
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
    }
}

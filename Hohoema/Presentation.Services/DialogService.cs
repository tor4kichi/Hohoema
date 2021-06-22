using I18NPortable;
using Hohoema.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Web.Http;
using Hohoema.Presentation.Views.Dialogs;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.UseCase.Playlist;

namespace Hohoema.Presentation.Services
{
    public class DialogService
    {
        public LoginUserOwnedMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public DialogService(
            )
        {
        }

        #region Update Notice Dialog

        public async Task ShowLatestUpdateNotice()
        {
            var text = await AppUpdateNotice.GetUpdateNoticeAsync();
            var dialog = new Dialogs.MarkdownTextDialog("UpdateNotice".Translate());
            dialog.Text = text;
            dialog.PrimaryButtonText = "Close".Translate();
            
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
                data.IsPublic = resultData.IsPublic;
                data.DefaultSortKey = resultData.DefaultSortKey;
                data.DefaultSortOrder = resultData.DefaultSortOrder;
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
        

        #region AdvancedSelectDialog

        public async Task<T> ShowSingleSelectDialogAsync<T>(
            List<T> sourceItems, 
            string displayMemberPath, 
            Func<T, string, bool> filter,
            string dialogTitle,
            string dialogPrimaryButtonText,
            string dialogSecondaryButtonText = null,
            Func<Task<T>> SecondaryButtonAction = null
            )
        {
            var advancedSelectDialog = new AdvancedSelectDialog();
            advancedSelectDialog.Title = dialogTitle;
            advancedSelectDialog.PrimaryButtonText = dialogPrimaryButtonText;
            advancedSelectDialog.CloseButtonText = "Cancel".Translate();
            advancedSelectDialog.SetSourceItems(sourceItems, filter != null ? (x, s) => filter((T)x, s) : default(Func<object, string, bool>));
            advancedSelectDialog.ItemDisplayMemberPath = displayMemberPath;
            advancedSelectDialog.IsMultiSelection = false;
            if (dialogSecondaryButtonText != null)
            {
                advancedSelectDialog.SecondaryButtonText = dialogSecondaryButtonText;
            }

            var result = await advancedSelectDialog.ShowAsync();
            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return (T)advancedSelectDialog.GetResultItems().FirstOrDefault();
            }
            else if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
            {
                return await SecondaryButtonAction?.Invoke();
            }
            else
            {
                return default(T);
            }
        }

        public async Task<List<T>> ShowMultiSelectDialogAsync<T>(
            List<T> sourceItems,
            string displayMemberPath,
            Func<T, string, bool> filter,
            string dialogTitle,
            string dialogPrimaryButtonText
            )
        {
            var advancedSelectDialog = new AdvancedSelectDialog();
            advancedSelectDialog.Title = dialogTitle;
            advancedSelectDialog.PrimaryButtonText = dialogPrimaryButtonText;
            advancedSelectDialog.SecondaryButtonText = "Cancel".Translate();
            advancedSelectDialog.SetSourceItems(sourceItems, filter != null ? (x, s) => filter((T)x, s) : default(Func<object, string, bool>));
            advancedSelectDialog.ItemDisplayMemberPath = displayMemberPath;
            advancedSelectDialog.IsMultiSelection = true;
            var result = await advancedSelectDialog.ShowAsync();
            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return advancedSelectDialog.GetResultItems().Cast<T>().ToList();
            }
            else
            {
                return new List<T>();
            }
        }



        #endregion

    }
}

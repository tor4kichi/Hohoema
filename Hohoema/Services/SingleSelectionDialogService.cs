using Hohoema.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal sealed class SingleSelectionDialogService : ISingleSelectionDialogService
    {

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
    }
}

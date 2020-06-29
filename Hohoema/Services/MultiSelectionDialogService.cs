using Hohoema.UseCase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal sealed class MultiSelectionDialogService : IMultiSelectionDialogService
    {

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

    }

}

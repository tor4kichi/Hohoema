using Hohoema.Dialogs;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal sealed class EditMylistGroupDialogService : IEditMylistGroupDialogService
    {

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

    }
}

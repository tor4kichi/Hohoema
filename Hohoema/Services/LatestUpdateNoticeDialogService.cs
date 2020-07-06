using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    public sealed class LatestUpdateNoticeDialogService 
    {

        public async Task ShowLatestUpdateNotice()
        {
            var text = await Models.Helpers.AppUpdateNotice.GetUpdateNoticeAsync();
            var dialog = new Dialogs.MarkdownTextDialog("UpdateNotice".Translate());
            dialog.Text = text;
            dialog.PrimaryButtonText = "Close".Translate();

            await dialog.ShowAsync();
        }

    }
}

using Hohoema.UseCase.Services;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Services
{
    internal sealed class ConfirmCacheUsageDialogService : IConfirmCacheUsageDialogService
    {
        static readonly string CacheUsageConfirmationFileUri = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\CacheUsageConfirmation.md";

        public async Task<bool> ShowAcceptCacheUsaseDialogAsync(bool showWithoutConfirmButton = false)
        {
            var dialog = new Hohoema.Dialogs.MarkdownTextDialog("HohoemaCacheVideoFairUsePolicy".Translate());


            var file = await StorageFile.GetFileFromPathAsync(CacheUsageConfirmationFileUri);
            dialog.Text = await FileIO.ReadTextAsync(file);

            if (!showWithoutConfirmButton)
            {
                dialog.PrimaryButtonText = "Accept".Translate();
                dialog.SecondaryButtonText = "Cancel".Translate();
            }
            else
            {
                dialog.PrimaryButtonText = "Close".Translate();
            }

            var result = await dialog.ShowAsync();

            return result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary;
        }

    }
}

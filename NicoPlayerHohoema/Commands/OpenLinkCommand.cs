using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenLinkCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            if (parameter is Uri)
            {
                return true;
            }

            if (parameter is string)
            {
                return Uri.TryCreate(parameter as string, UriKind.Absolute, out var uri);
            }

            return false;
        }

        protected override void Execute(object parameter)
        {
            var uri = parameter as Uri;

            if (parameter is string)
            {
                uri = new Uri(parameter as string);
            }

            if (uri != null)
            {
                Launcher.LaunchUriAsync(uri).AsTask().ConfigureAwait(false);
            }
        }
    }
}

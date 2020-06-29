using Hohoema.Models.Helpers;
using Hohoema.Models.Repository;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.ExternalAccess.Commands
{
    public sealed class OpenLinkCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object content)
        {
            if (content is INiconicoObject) { return true; }
            else if (content is Uri) { return true; }
            else if (content is string) { return Uri.TryCreate(content as string, UriKind.Absolute, out var uri); }
            else { return false; }
        }

        protected override void Execute(object content)
        {
            Uri uri = null;
            if (content is INiconicoObject niconicoContent)
            {
                uri = ExternalAccessHelper.ConvertToUrl(niconicoContent);

                // TODO: 
            }
            else if (content is Uri uriContent)
            {
                uri = uriContent;
            }
            else if (content is string str)
            {
                uri = new Uri(str);
            }

            if (uri != null)
            {
                _ = Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }
    }
}

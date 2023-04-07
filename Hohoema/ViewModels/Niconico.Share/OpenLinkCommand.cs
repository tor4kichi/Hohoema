using Hohoema.Models.Niconico;
using Hohoema.Helpers;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Share
{
    public sealed class OpenLinkCommand : CommandBase
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
                uri = ShareHelper.ConvertToUrl(niconicoContent);

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

                //Analytics.TrackEvent("OpenLinkCommand", new Dictionary<string, string>
                //{

                //});
            }
        }
    }
}

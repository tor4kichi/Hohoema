using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Helpers;
using Microsoft.AppCenter.Analytics;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Share
{
    public sealed class OpenShareUICommand : DelegateCommandBase
    {
        protected override bool CanExecute(object content)
        {
            return Windows.ApplicationModel.DataTransfer.DataTransferManager.IsSupported()
                        && (content as INiconicoContent)?.Id != null;
        }

        protected override void Execute(object content)
        {
            var shareContent = ShareHelper.MakeShareText(content as INiconicoContent);
            ShareHelper.Share(shareContent);

            Analytics.TrackEvent("OpenShareUICommand", new Dictionary<string, string>
                {
                    { "ContentType", content.GetType().Name }
                });
        }
    }
}

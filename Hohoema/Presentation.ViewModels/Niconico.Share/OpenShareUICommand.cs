using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Share
{
    public sealed class OpenShareUICommand : CommandBase
    {
        public OpenShareUICommand(AppearanceSettings appearanceSettings)
        {

        }

        protected override bool CanExecute(object content)
        {
            return Windows.ApplicationModel.DataTransfer.DataTransferManager.IsSupported()
                        && content is INiconicoObject;
        }

        protected override void Execute(object content)
        {
            if (content is INiconicoObject nicoContent)
            {
                ShareHelper.Share(nicoContent);

                //Analytics.TrackEvent("OpenShareUICommand", new Dictionary<string, string>
                //{
                //    { "ContentType", content.GetType().Name }
                //});
            }
        }
    }
}

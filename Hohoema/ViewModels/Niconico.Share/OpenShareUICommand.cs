using Hohoema.Models.Application;
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

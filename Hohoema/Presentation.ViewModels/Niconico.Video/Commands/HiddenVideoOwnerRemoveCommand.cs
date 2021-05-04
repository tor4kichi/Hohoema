using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class HiddenVideoOwnerRemoveCommand : DelegateCommandBase
    {
        private readonly VideoFilteringSettings _ngSettings;

        public HiddenVideoOwnerRemoveCommand(VideoFilteringSettings ngSettings)
        {
            _ngSettings = ngSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is IVideoContentProvider provider)
            {
                if (provider.ProviderId != null)
                {
                    return _ngSettings.IsHiddenVideoOwnerId(provider.ProviderId);
                }
            }

            return false;
        }

        protected override void Execute(object parameter)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            if (parameter is IVideoContentProvider provider)
            {
                if (provider.ProviderId != null)
                {
                    _ngSettings.RemoveHiddenVideoOwnerId(provider.ProviderId);
                }
            }
        }
    }
}

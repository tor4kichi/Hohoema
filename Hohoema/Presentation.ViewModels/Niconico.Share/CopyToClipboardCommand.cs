using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Helpers;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Share
{
    public sealed class CopyToClipboardCommand : CommandBase
    {
        private readonly NotificationService _notificationService;

        public CopyToClipboardCommand(
            NotificationService notificationService
            )
        {
            _notificationService = notificationService;
        }
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object content)
        {
            if (content is INiconicoObject niconicoContent)
            {
                var uri = ShareHelper.ConvertToUrl(niconicoContent);
                ClipboardHelper.CopyToClipboard(uri.OriginalString);
            }
            else
            {
                ClipboardHelper.CopyToClipboard(content.ToString());
            }

            _notificationService.ShowLiteInAppNotification_Success("Copy".Translate());

            //Analytics.TrackEvent("CopyToClipboardCommand", new Dictionary<string, string>
            //{

            //});
        }
    }
}

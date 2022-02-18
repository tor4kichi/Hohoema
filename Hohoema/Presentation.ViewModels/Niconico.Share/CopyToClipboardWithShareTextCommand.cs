using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Helpers;
using Hohoema.Presentation.Services;
using I18NPortable;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Share
{
    public sealed class CopyToClipboardWithShareTextCommand : DelegateCommandBase
    {
        private readonly NotificationService _notificationService;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public CopyToClipboardWithShareTextCommand(
            NotificationService notificationService,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _notificationService = notificationService;
            _nicoVideoProvider = nicoVideoProvider;
        }
        protected override bool CanExecute(object parameter)
        {
            return true;

        }

        protected override void Execute(object content)
        {
            if (content is INiconicoContent niconicoContent)
            {
                var shareContent = ShareHelper.MakeShareTextWithTitle(niconicoContent);
                ClipboardHelper.CopyToClipboard(shareContent);
            }
            else if (content is string contentId)
            {
                var video = _nicoVideoProvider.GetCachedVideoInfo(contentId);
                if (video != null)
                {
                    var shareContent = ShareHelper.MakeShareTextWithTitle(video);
                    ClipboardHelper.CopyToClipboard(shareContent);
                }
            }
            else
            {
                ClipboardHelper.CopyToClipboard(content.ToString());
            }

            _notificationService.ShowLiteInAppNotification_Success("Copy".Translate());

            //Analytics.TrackEvent("CopyToClipboardWithShareTextCommand", new Dictionary<string, string>
            //{

            //});
        }
    }
}

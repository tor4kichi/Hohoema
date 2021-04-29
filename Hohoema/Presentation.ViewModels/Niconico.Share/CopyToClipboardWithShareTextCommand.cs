using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Helpers;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.AppCenter.Analytics;
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
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NotificationService _notificationService;

        public CopyToClipboardWithShareTextCommand(
            NicoVideoCacheRepository nicoVideoRepository,
            NotificationService notificationService
            )
        {
            _nicoVideoRepository = nicoVideoRepository;
            _notificationService = notificationService;
        }
        protected override bool CanExecute(object parameter)
        {
            return true;

        }

        protected override void Execute(object content)
        {
            if (content is INiconicoContent niconicoContent)
            {
                var shareContent = ShareHelper.MakeShareText(niconicoContent);
                ClipboardHelper.CopyToClipboard(shareContent);
            }
            else if (content is string contentId)
            {
                var video = _nicoVideoRepository.Get(contentId);
                if (video != null)
                {
                    var shareContent = ShareHelper.MakeShareText(video);
                    ClipboardHelper.CopyToClipboard(shareContent);
                }
            }
            else
            {
                ClipboardHelper.CopyToClipboard(content.ToString());
            }

            _notificationService.ShowLiteInAppNotification_Success("Copy".Translate());

            Analytics.TrackEvent("CopyToClipboardWithShareTextCommand", new Dictionary<string, string>
            {

            });
        }
    }
}

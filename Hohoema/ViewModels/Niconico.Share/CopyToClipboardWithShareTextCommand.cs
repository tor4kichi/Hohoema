#nullable enable
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Services;
using I18NPortable;

namespace Hohoema.ViewModels.Niconico.Share;

public sealed class CopyToClipboardWithShareTextCommand : CommandBase
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

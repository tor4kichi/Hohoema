#nullable enable
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico;

namespace Hohoema.ViewModels.Niconico.Share;

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

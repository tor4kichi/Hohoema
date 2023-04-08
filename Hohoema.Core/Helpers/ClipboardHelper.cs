using Hohoema.Models.Niconico.Video;
using NiconicoToolkit;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Hohoema.Helpers;

public static class ClipboardHelper
{
    public static void CopyToClipboard(string content)
    {
        DataPackage datapackage = new();
        datapackage.SetText(content);

        // アプリのクリップボードチェックアクションをアプリ内部からのコピーでは作動させないようにする
        SetIgnoreClipboardCheckingOnce(content);

        Clipboard.SetContent(datapackage);
    }

    public static void CopyToClipboard(NicoVideo video)
    {
        CopyToClipboard(Helpers.ShareHelper.MakeShareTextWithTitle(video));
    }


    private static string prevContent = string.Empty;
    public static void SetIgnoreClipboardCheckingOnce(string ignoredContent)
    {
        prevContent = ignoredContent;
    }


    public static async Task<NiconicoId?> CheckClipboard()
    {
        DataPackageView dataPackageView = Clipboard.GetContent();
        if (dataPackageView.Contains(StandardDataFormats.WebLink))
        {
            Uri uri = await dataPackageView.GetWebLinkAsync();
            if (uri.OriginalString == prevContent) { return null; }

            prevContent = uri.OriginalString;
            return NiconicoUrls.ExtractNicoContentId(uri);
        }
        else if (dataPackageView.Contains(StandardDataFormats.Text))
        {
            string text = await dataPackageView.GetTextAsync();
            if (prevContent == text) { return null; }
            prevContent = text;
            try
            {
                return Uri.TryCreate(text, UriKind.Absolute, out Uri uri)
                    ? NiconicoUrls.ExtractNicoContentId(uri)
                    : NiconicoId.TryCreate(text, out NiconicoId id) ? id : null;
            }
            catch
            {

            }
        }

        return null;
    }
}

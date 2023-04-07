using Hohoema.Models.Niconico.Video;
using NiconicoToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Hohoema.Helpers
{


    static public class ClipboardHelper
    {
        static public void CopyToClipboard(string content)
        {
            var datapackage = new DataPackage();
            datapackage.SetText(content);

            // アプリのクリップボードチェックアクションをアプリ内部からのコピーでは作動させないようにする
            SetIgnoreClipboardCheckingOnce(content);

            Clipboard.SetContent(datapackage);
        }

        static public void CopyToClipboard(NicoVideo video)
        {
            CopyToClipboard(Helpers.ShareHelper.MakeShareTextWithTitle(video));
        }


        static private string prevContent = string.Empty;
        static public void SetIgnoreClipboardCheckingOnce(string ignoredContent)
        {
            prevContent = ignoredContent;
        }


        static public async Task<NiconicoId?> CheckClipboard()
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await dataPackageView.GetWebLinkAsync();
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
                    if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
                    {
                        return NiconicoUrls.ExtractNicoContentId(uri);
                    }
                    else
                    {
                        return NiconicoId.TryCreate(text, out var id) ? id : null;
                    }
                }
                catch
                {

                }
            }

            return null;
        }
    }
}

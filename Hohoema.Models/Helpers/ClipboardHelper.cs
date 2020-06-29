using Hohoema.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Hohoema.Models.Helpers
{
    public enum ContentType
    {
        Video,
        Live,
        Mylist,
        Community,
        User,
        Channel,
    }


    public class ClipboardDetectedEventArgs
    {
        public ContentType Type { get; set; }
        public string Id { get; set; }
    }

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

        static public void CopyToClipboard(IVideoContent video)
        {
            CopyToClipboard(ShareHelper.MakeShareText(video));
        }

        static public void CopyToClipboard(ILiveContent video)
        {
            CopyToClipboard(ShareHelper.MakeShareText(video));
        }

        static readonly Regex NicoContentRegex = new Regex("https?:\\/\\/([\\w\\W]*?)\\/((\\w*)\\/)?([\\w-]*)");

        static private string prevContent = string.Empty;
        static public void SetIgnoreClipboardCheckingOnce(string ignoredContent)
        {
            prevContent = ignoredContent;
        }


        static public async Task<ClipboardDetectedEventArgs> CheckClipboard()
        {
            ClipboardDetectedEventArgs clipboardValue = null;

            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await dataPackageView.GetWebLinkAsync();
                if (uri.OriginalString == prevContent) { return null; }

                clipboardValue = ExtractNicoContentId(uri);

                prevContent = uri.OriginalString;
            }
            else if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                if (prevContent == text) { return null; }
                try
                {
                    if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
                    {
                        clipboardValue = ExtractNicoContentId(uri);
                    }
                    else
                    {
                        clipboardValue = ExtractNicoContentId(text);
                    }
                }
                catch
                {

                }
                prevContent = text;
            }

            return clipboardValue;
        }

        static private ClipboardDetectedEventArgs ExtractNicoContentId(string contentId)
        {
            ClipboardDetectedEventArgs clipboardValue = null;

            if (Mntone.Nico2.NiconicoRegex.IsVideoId(contentId))
            {
                clipboardValue = new ClipboardDetectedEventArgs()
                {
                    Type = ContentType.Video,
                    Id = contentId
                };
            }
            else if (Mntone.Nico2.NiconicoRegex.IsLiveId(contentId))
            {
                clipboardValue = new ClipboardDetectedEventArgs()
                {
                    Type = ContentType.Live,
                    Id = contentId
                };
            }

            return clipboardValue;
        }

        static private ClipboardDetectedEventArgs ExtractNicoContentId(Uri url)
        {
            ClipboardDetectedEventArgs clipboardValue = null;

            var match = NicoContentRegex.Match(url.OriginalString);
            if (match.Success)
            {
                var hostNameGroup = match.Groups[1];
                var contentTypeGroup = match.Groups[3];
                var contentIdGroup = match.Groups[4];

                var contentId = contentIdGroup.Value;

                if (Mntone.Nico2.NiconicoRegex.IsVideoId(contentId))
                {
                    clipboardValue = new ClipboardDetectedEventArgs()
                    {
                        Type = ContentType.Video,
                        Id = contentId
                    };
                }
                else if (Mntone.Nico2.NiconicoRegex.IsLiveId(contentId))
                {
                    clipboardValue = new ClipboardDetectedEventArgs()
                    {
                        Type = ContentType.Live,
                        Id = contentId
                    };
                }
                else if (contentTypeGroup.Success)
                {
                    var contentType = contentTypeGroup.Value;
                    switch (contentType)
                    {
                        case "watch":
                            clipboardValue = new ClipboardDetectedEventArgs()
                            {
                                Type = ContentType.Video,
                                Id = contentId
                            };
                            break;
                        case "mylist":
                            clipboardValue = new ClipboardDetectedEventArgs()
                            {
                                Type = ContentType.Mylist,
                                Id = contentId
                            };
                            break;
                        case "community":
                            clipboardValue = new ClipboardDetectedEventArgs()
                            {
                                Type = ContentType.Community,
                                Id = contentId
                            };
                            break;
                        case "user":
                            clipboardValue = new ClipboardDetectedEventArgs()
                            {
                                Type = ContentType.User,
                                Id = contentId
                            };
                            break;
                    }
                }
                else if (hostNameGroup.Success)
                {
                    var hostName = hostNameGroup.Value;

                    if (hostName == "ch.nicovideo.jp")
                    {
                        // TODO: クリップボードから受け取ったチャンネルIdを開く
                        clipboardValue = new ClipboardDetectedEventArgs()
                        {
                            Type = ContentType.Channel,
                            Id = contentId
                        };
                    }
                }
            }

            return clipboardValue;
        }
    }
}

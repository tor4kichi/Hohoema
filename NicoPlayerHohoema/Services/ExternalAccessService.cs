using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services
{
    public sealed class ExternalAccessService : BindableBase
    {

        static private Uri ConvertToUrl(Interfaces.INiconicoObject content)
        {
            Uri uri = null;
            switch (content)
            {
                case Interfaces.IUser user:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeUserPageUrl(user.Id)));
                    break;
                case Interfaces.IVideoContent videoContent:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl, videoContent.Id));
                    break;
                case Interfaces.IMylistItem mylist:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeMylistPageUrl(mylist.Id)));
                    break;
                case Interfaces.ILiveContent live:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.LiveWatchPageUrl, live.Id));
                    break;
                case Interfaces.IChannel channel:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.ChannelUrlBase, channel.Id));
                    break;
                case Interfaces.ICommunity community:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.CommynitySammaryPageUrl, community.Id));
                    break;

                default:
                    break;
            }

            // TODO: ConvertToUrl、INiconicoContent派生のクラスに対応

            return uri;
        }

        // Clipboard


        private DelegateCommand<object> _CopyToClipboardCommand;
        public DelegateCommand<object> CopyToClipboardCommand => _CopyToClipboardCommand
            ?? (_CopyToClipboardCommand = new DelegateCommand<object>(content =>
            {
                if (content is Interfaces.INiconicoObject niconicoContent)
                {
                    var uri = ConvertToUrl(niconicoContent);
                    Helpers.ClipboardHelper.CopyToClipboard(uri.OriginalString);
                }
                else
                {
                    Helpers.ClipboardHelper.CopyToClipboard(content.ToString());
                }
            }
            , content => content != null
            ));

        private DelegateCommand<object> _CopyToClipboardWithShareTextCommand;
        public DelegateCommand<object> CopyToClipboardWithShareTextCommand => _CopyToClipboardWithShareTextCommand
            ?? (_CopyToClipboardWithShareTextCommand = new DelegateCommand<object>(content =>
            {
                if (content is Interfaces.INiconicoContent niconicoContent)
                {
                    var shareContent = Services.Helpers.ShareHelper.MakeShareText(niconicoContent);
                    Helpers.ClipboardHelper.CopyToClipboard(shareContent);
                }
                else if (content is string contentId)
                {
                    var video = Database.NicoVideoDb.Get(contentId);
                    if (video != null)
                    {
                        var shareContent = Services.Helpers.ShareHelper.MakeShareText(video);
                        Helpers.ClipboardHelper.CopyToClipboard(shareContent);
                    }
                }
                else
                {
                    Helpers.ClipboardHelper.CopyToClipboard(content.ToString());
                }
            }
            , content => content != null
            ));

        private DelegateCommand<object> _OpenLinkCommand;
        public DelegateCommand<object> OpenLinkCommand => _OpenLinkCommand
            ?? (_OpenLinkCommand = new DelegateCommand<object>(content =>
            {
                Uri uri = null;
                if (content is Interfaces.INiconicoObject niconicoContent)
                {
                    uri = ConvertToUrl(niconicoContent);

                    // TODO: 
                }
                else if (content is Uri uriContent)
                {
                    uri = uriContent;
                }
                else if (content is string str)
                {
                    uri = new Uri(str);
                }

                if (uri != null)
                {
                    _ = Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
            , content =>
            {
                if (content is Interfaces.INiconicoObject) { return true; }
                else if (content is Uri) { return true; }
                else if (content is string) { return Uri.TryCreate(content as string, UriKind.Absolute, out var uri); }
                else { return false; }
            }
            ));

        
        private DelegateCommand<Interfaces.INiconicoContent> _OpenShareUICommand;
        public DelegateCommand<Interfaces.INiconicoContent> OpenShareUICommand => _OpenShareUICommand
            ?? (_OpenShareUICommand = new DelegateCommand<Interfaces.INiconicoContent>(content => 
            {
                var shareContent = Services.Helpers.ShareHelper.MakeShareText(content);
                Services.Helpers.ShareHelper.Share(shareContent);
            }
            , content => Windows.ApplicationModel.DataTransfer.DataTransferManager.IsSupported() 
                        && content?.Id != null
            ));
    }
}

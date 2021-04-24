using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using I18NPortable;
using Microsoft.AppCenter.Analytics;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.Services
{
    public sealed class ExternalAccessService : BindableBase
    {
        public ExternalAccessService(
            NicoVideoCacheRepository nicoVideoRepository,
            NotificationService notificationService
            )
        {
            _nicoVideoRepository = nicoVideoRepository;
            _notificationService = notificationService;
        }

        static private Uri ConvertToUrl(INiconicoObject content)
        {
            Uri uri = null;
            switch (content)
            {
                case IUser user:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeUserPageUrl(user.Id)));
                    break;
                case IVideoContent videoContent:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl, videoContent.Id));
                    break;
                case IMylist mylist:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeMylistPageUrl(mylist.Id)));
                    break;
                case ILiveContent live:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.LiveWatchPageUrl, live.Id));
                    break;
                case IChannel channel:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.ChannelUrlBase, "channel",  "ch" + channel.Id));
                    break;
                case ICommunity community:
                    uri = new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.CommynitySammaryPageUrl, community.Id));
                    break;

                default:
                    break;
            }

            return uri;
        }

        // Clipboard


        private DelegateCommand<object> _CopyToClipboardCommand;
        public DelegateCommand<object> CopyToClipboardCommand => _CopyToClipboardCommand
            ?? (_CopyToClipboardCommand = new DelegateCommand<object>(content =>
            {
                if (content is INiconicoObject niconicoContent)
                {
                    var uri = ConvertToUrl(niconicoContent);
                    Helpers.ClipboardHelper.CopyToClipboard(uri.OriginalString);
                }
                else
                {
                    Helpers.ClipboardHelper.CopyToClipboard(content.ToString());
                }

                _notificationService.ShowLiteInAppNotification_Success("Copy".Translate());

                Analytics.TrackEvent("CopyToClipboardCommand", new Dictionary<string, string>
                {

                });
            }
            , content => content != null
            ));

        private DelegateCommand<object> _CopyToClipboardWithShareTextCommand;
        public DelegateCommand<object> CopyToClipboardWithShareTextCommand => _CopyToClipboardWithShareTextCommand
            ?? (_CopyToClipboardWithShareTextCommand = new DelegateCommand<object>(content =>
            {
                if (content is INiconicoContent niconicoContent)
                {
                    var shareContent = Services.Helpers.ShareHelper.MakeShareText(niconicoContent);
                    Helpers.ClipboardHelper.CopyToClipboard(shareContent);
                }
                else if (content is string contentId)
                {
                    var video = _nicoVideoRepository.Get(contentId);
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

                _notificationService.ShowLiteInAppNotification_Success("Copy".Translate());

                Analytics.TrackEvent("CopyToClipboardWithShareTextCommand", new Dictionary<string, string>
                {

                });
            }
            , content => content != null
            ));

        private DelegateCommand<object> _OpenLinkCommand;
        public DelegateCommand<object> OpenLinkCommand => _OpenLinkCommand
            ?? (_OpenLinkCommand = new DelegateCommand<object>(content =>
            {
                OpenLink(content);
            }
            , content =>
            {
                if (content is INiconicoObject) { return true; }
                else if (content is Uri) { return true; }
                else if (content is string) { return Uri.TryCreate(content as string, UriKind.Absolute, out var uri); }
                else { return false; }
            }
            ));

        public void OpenLink(object content)
        {
            Uri uri = null;
            if (content is INiconicoObject niconicoContent)
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

                Analytics.TrackEvent("OpenLinkCommand", new Dictionary<string, string>
                {

                });
            }
        }
        
        private DelegateCommand<INiconicoContent> _OpenShareUICommand;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NotificationService _notificationService;

        public DelegateCommand<INiconicoContent> OpenShareUICommand => _OpenShareUICommand
            ?? (_OpenShareUICommand = new DelegateCommand<INiconicoContent>(content => 
            {
                var shareContent = Services.Helpers.ShareHelper.MakeShareText(content);
                Services.Helpers.ShareHelper.Share(shareContent);

                Analytics.TrackEvent("OpenShareUICommand", new Dictionary<string, string> 
                {
                    { "ContentType", content.GetType().Name }
                });
            }
            , content => Windows.ApplicationModel.DataTransfer.DataTransferManager.IsSupported() 
                        && content?.Id != null
            ));
    }
}

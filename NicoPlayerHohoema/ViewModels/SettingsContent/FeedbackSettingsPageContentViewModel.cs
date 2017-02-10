using Microsoft.Services.Store.Engagement;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace NicoPlayerHohoema.ViewModels
{
    public class FeedbackSettingsPageContentViewModel : SettingsPageContentViewModel
    {
        public FeedbackSettingsPageContentViewModel()
            : base("フィードバック", HohoemaSettingsKind.Feedback)
        {
            IsSupportedFeedbackHub = StoreServicesFeedbackLauncher.IsSupported();
        }

        public override void OnLeave()
        {
            // do nothing
        }

        private static Uri AppIssuePageUri = new Uri("https://github.com/tor4kichi/Hohoema/issues");
        private static Uri AppReviewUri = new Uri("ms-windows-store://review/?ProductId=9nblggh4rxt6");


        public bool IsSupportedFeedbackHub { get; private set; }

        private DelegateCommand _LaunchAppReviewCommand;
        public DelegateCommand LaunchAppReviewCommand
        {
            get
            {
                return _LaunchAppReviewCommand
                    ?? (_LaunchAppReviewCommand = new DelegateCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(AppReviewUri);
                    }));
            }
        }

        private DelegateCommand _LaunchFeedbackHubCommand;
        public DelegateCommand LaunchFeedbackHubCommand
        {
            get
            {
                return _LaunchFeedbackHubCommand
                    ?? (_LaunchFeedbackHubCommand = new DelegateCommand(async () =>
                    {
                        if (IsSupportedFeedbackHub)
                        {
                            await StoreServicesFeedbackLauncher.GetDefault().LaunchAsync();
                        }
                    }));
            }
        }

        private DelegateCommand _ShowIssuesWithBrowserCommand;
        public DelegateCommand ShowIssuesWithBrowserCommand
        {
            get
            {
                return _ShowIssuesWithBrowserCommand
                    ?? (_ShowIssuesWithBrowserCommand = new DelegateCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(AppIssuePageUri);
                    }));
            }
        }

    }
}

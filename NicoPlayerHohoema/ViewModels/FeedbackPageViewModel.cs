using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Microsoft.Services.Store.Engagement;
using Windows.System;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedbackPageViewModel : HohoemaViewModelBase
	{
		private static Uri AppIssuePageUri = new Uri("https://github.com/tor4kichi/Hohoema/issues");
		private static Uri AppReviewUri = new Uri("ms-windows-store://review/?ProductId=9nblggh4rxt6");


		public bool IsSupportedFeedbackHub { get; private set; }

		public FeedbackPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn:false)
		{
			IsSupportedFeedbackHub = StoreServicesFeedbackLauncher.IsSupported();
		}

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

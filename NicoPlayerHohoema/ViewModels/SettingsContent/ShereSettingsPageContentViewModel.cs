using NicoPlayerHohoema.Util;
using Prism.Commands;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public sealed class ShereSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public ReactiveProperty<bool> IsLoginTwitter { get; private set; }
		public ReactiveProperty<string> TwitterAccountScreenName { get; private set; }

		public ShereSettingsPageContentViewModel() 
			: base("SNS連携", HohoemaSettingsKind.Share)
		{
			IsLoginTwitter = new ReactiveProperty<bool>(TwitterHelper.IsLoggedIn);
			TwitterAccountScreenName = new ReactiveProperty<string>(TwitterHelper.TwitterUser?.ScreenName ?? "");
		}

		public override void OnLeave()
		{
			
		}

		private DelegateCommand _LogInToTwitterCommand;
		public DelegateCommand LogInToTwitterCommand
		{
			get
			{
				return _LogInToTwitterCommand
					?? (_LogInToTwitterCommand = new DelegateCommand(async () =>
					{
						if (await TwitterHelper.LoginOrRefreshToken())
						{
							IsLoginTwitter.Value = TwitterHelper.IsLoggedIn;
							TwitterAccountScreenName.Value = TwitterHelper.TwitterUser?.ScreenName ?? "";
						}
					}
					));
			}
		}

		private DelegateCommand _LogoutTwitterCommand;
		public DelegateCommand LogoutTwitterCommand
		{
			get
			{
				return _LogoutTwitterCommand
					?? (_LogoutTwitterCommand = new DelegateCommand(() =>
					{
						TwitterHelper.Logout();

						IsLoginTwitter.Value = false;
						TwitterAccountScreenName.Value = "";
					}
					));
			}
		}
	}
}

using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaViewModelBase : ViewModelBase
	{
		public HohoemaViewModelBase(HohoemaApp hohoemaApp, PageManager pageManager, bool isRequireSignIn = false)
		{
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			IsRequireSignIn = isRequireSignIn;

			HohoemaApp.OnSignout += HohoemaApp_OnSignout;
			HohoemaApp.OnSignin += HohoemaApp_OnSignin;

		}

		private void HohoemaApp_OnSignin()
		{
			OnSignIn();
		}

		private void HohoemaApp_OnSignout()
		{
			OnSignOut();
		}

		

		protected virtual void OnSignIn()
		{
			NowSignIn = true;
		}

		protected virtual void OnSignOut()
		{
			NowSignIn = false;
		}

		protected async Task<bool> CheckSignIn()
		{
			return await HohoemaApp.CheckSignedInStatus() == Mntone.Nico2.NiconicoSignInStatus.Success;
		}

		abstract public string GetPageTitle();


		private DelegateCommand _BackCommand;
		public DelegateCommand BackCommand
		{
			get
			{
				return _BackCommand
					?? (_BackCommand = new DelegateCommand(
						() => 
						{
							if (PageManager.NavigationService.CanGoBack())
							{
								PageManager.NavigationService.GoBack();
							}
							else
							{
								PageManager.OpenPage(HohoemaPageType.Portal);
							}
						}));
			}
		}


		public bool IsRequireSignIn { get; private set; }
		public bool NowSignIn { get; private set; }


		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }
	}
}

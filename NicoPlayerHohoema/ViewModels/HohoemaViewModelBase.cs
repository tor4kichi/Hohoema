using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Reactive.Disposables;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaViewModelBase : ViewModelBase, IDisposable
	{
		// TODO: サインインライフサイクルの確実な呼び出しをサポートする

		// Note: サインイン後にHohoemaViewModelBaseが呼び出された場合、OnSigninが呼び出されない。これに対処する


		public HohoemaViewModelBase(HohoemaApp hohoemaApp, PageManager pageManager, bool isRequireSignIn = false)
		{
			_SignStatusLock = new SemaphoreSlim(1, 1);
			_NavigationToLock = new SemaphoreSlim(1, 1);
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			IsRequireSignIn = isRequireSignIn;
			NowSignIn = false;

			HohoemaApp.OnSignout += OnSignout;
			HohoemaApp.OnSignin += OnSignin;

			_CompositeDisposable = new CompositeDisposable();

			_UserSettingsCompositeDisposable = new CompositeDisposable();

			OnSignin();
		}

		private void OnSignin()
		{
			try
			{
				_SignStatusLock.Wait();

				if (!NowSignIn && HohoemaApp.IsLoggedIn)
				{
					_UserSettingsCompositeDisposable?.Dispose();
					_UserSettingsCompositeDisposable = new CompositeDisposable();

					NowSignIn = true;

					OnSignIn(_UserSettingsCompositeDisposable);
				}
			}
			finally
			{
				_SignStatusLock.Release();
			}			
		}

		private void OnSignout()
		{
			try
			{
				_SignStatusLock.Wait();

				if (NowSignIn && !HohoemaApp.IsLoggedIn)
				{
					NowSignIn = false;

					OnSignOut();

					_UserSettingsCompositeDisposable?.Dispose();
					_UserSettingsCompositeDisposable = null;
				}
			}
			finally
			{
				_SignStatusLock.Release();
			}
		}

		

		protected virtual void OnSignIn(ICollection<IDisposable> userSessionDisposer)
		{
		}

		protected virtual void OnSignOut()
		{
		}

		protected async Task<bool> CheckSignIn()
		{
			return await HohoemaApp.CheckSignedInStatus() == Mntone.Nico2.NiconicoSignInStatus.Success;
		}

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

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			_IsNavigatedToDone = false;
			_IsNavigatingFromDone = false;

			base.OnNavigatedTo(e, viewModelState);

			if (!String.IsNullOrEmpty(_Title))
			{
				PageManager.PageTitle = _Title;
			}
			else
			{
				PageManager.PageTitle = PageManager.CurrentDefaultPageTitle();
			}

			try
			{
				await _NavigationToLock.WaitAsync();

				await OnNavigatedToAsync(e, viewModelState);
			}
			finally
			{
				_NavigationToLock.Release();
			}

			_IsNavigatedToDone = true;
			_IsNavigatingFromDone = false;
		}


		protected virtual Task OnNavigatedToAsync(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return Task.CompletedTask;
		}

		public override async void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			while (!_IsNavigatedToDone)
			{
				await Task.Delay(50);
			}

			base.OnNavigatingFrom(e, viewModelState, suspending);

			try
			{
				await _NavigationToLock.WaitAsync();

				await OnNavigatingFromAsync(e, viewModelState, suspending);
			}
			finally
			{
				_NavigationToLock.Release();
			}

			if (suspending)
			{
				await HohoemaApp.MediaManager.Context.Suspending();
			}

			_IsNavigatingFromDone = true;
		}

		protected virtual Task OnNavigatingFromAsync(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			return Task.CompletedTask;
		}

		protected void UpdateTitle(string title)
		{
			_Title = title;
			PageManager.UpdateTitle(title);
		}

		public async void Dispose()
		{
			IsDisposed = true;

			while (!_IsNavigatingFromDone)
			{
				await Task.Delay(50);
			}

			OnSignout();

			try
			{
				_NavigationToLock.Wait();

				OnDispose();
			}
			finally
			{
				_NavigationToLock.Release();
			}

			_CompositeDisposable.Dispose();
			_UserSettingsCompositeDisposable?.Dispose();
		}


		private SemaphoreSlim _NavigationToLock;


		public bool IsDisposed { get; private set; }

		protected virtual void OnDispose() { }


		private SemaphoreSlim _SignStatusLock;

		private bool _IsNavigatedToDone;
		private bool _IsNavigatingFromDone;

		public bool IsRequireSignIn { get; private set; }
		public bool NowSignIn { get; private set; }

		private string _Title;

		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		protected CompositeDisposable _CompositeDisposable { get; private set; }
		protected CompositeDisposable _UserSettingsCompositeDisposable { get; private set; }
	}
}

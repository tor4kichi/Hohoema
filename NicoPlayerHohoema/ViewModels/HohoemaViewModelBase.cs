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
using Windows.UI.Xaml;
using Windows.Foundation;
using NicoPlayerHohoema.Util;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaViewModelBase : ViewModelBase, IDisposable
	{
		// TODO: サインインライフサイクルの確実な呼び出しをサポートする

		// Note: サインイン後にHohoemaViewModelBaseが呼び出された場合、OnSigninが呼び出されない。これに対処する


		public HohoemaViewModelBase(HohoemaApp hohoemaApp, PageManager pageManager, bool isRequireSignIn = true)
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

			HohoemaApp.OnResumed += _OnResumed;

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
			base.OnNavigatedTo(e, viewModelState);

			// サインインステータスチェック

			if (IsRequireSignIn)
			{
				if (!await CheckSignIn())
				{
					var result = await HohoemaApp.SignInFromUserSettings();

					if (result != Mntone.Nico2.NiconicoSignInStatus.Success)
					{
						// サインイン出来ない場合はログインページへ戻す
						PageManager.OpenPage(HohoemaPageType.Login);
						return;
					}
				}

				OnSignin();
			}


			_NavigatedToTaskCancelToken = new CancellationTokenSource();

			_NavigatedToTask = NavigatedToAsync(_NavigatedToTaskCancelToken.Token, e, viewModelState);

			if (!String.IsNullOrEmpty(_Title))
			{
				PageManager.PageTitle = _Title;
			}
			else
			{
				PageManager.PageTitle = PageManager.CurrentDefaultPageTitle();
			}
		}

		private void _OnResumed()
		{
			OnResumed().ConfigureAwait(false);
		}

		protected virtual Task OnResumed()
		{
			return Task.CompletedTask;
		}

		protected virtual Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return Task.CompletedTask;
		}


		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			_NavigatedToTaskCancelToken?.Cancel();

			var task = _NavigatedToTask.WaitToCompelation();

			task.Wait();

			_NavigatedToTaskCancelToken?.Dispose();
			_NavigatedToTaskCancelToken = null;

			base.OnNavigatingFrom(e, viewModelState, suspending);

		}

	
		protected void UpdateTitle(string title)
		{
			_Title = title;
			PageManager.UpdateTitle(title);
		}

		public void Dispose()
		{
			IsDisposed = true;
			
			if (IsRequireSignIn)
			{
				OnSignout();
			}

			OnDispose();

			_CompositeDisposable?.Dispose();
			_UserSettingsCompositeDisposable?.Dispose();

			HohoemaApp.OnSignout -= OnSignout;
			HohoemaApp.OnSignin -= OnSignin;

			HohoemaApp.OnResumed -= _OnResumed;
		}


		CancellationTokenSource _NavigatedToTaskCancelToken;
		Task _NavigatedToTask;

		private SemaphoreSlim _NavigationToLock;


		public bool IsDisposed { get; private set; }

		protected virtual void OnDispose() { }


		private SemaphoreSlim _SignStatusLock;


		public bool IsRequireSignIn { get; private set; }
		public bool NowSignIn { get; private set; }

		private string _Title;

		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		protected CompositeDisposable _CompositeDisposable { get; private set; }
		protected CompositeDisposable _UserSettingsCompositeDisposable { get; private set; }
	}
}

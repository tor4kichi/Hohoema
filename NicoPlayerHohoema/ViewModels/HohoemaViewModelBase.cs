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
using System.Runtime.InteropServices.WindowsRuntime;
using WinRTXamlToolkit.Async;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaViewModelBase : ViewModelBase, IDisposable
	{

		static Util.AsyncLock _NavigationLock = new Util.AsyncLock();

		public HohoemaViewModelBase(HohoemaApp hohoemaApp, PageManager pageManager, bool isRequireSignIn = true)
		{
			_SignStatusLock = new SemaphoreSlim(1, 1);
			_NavigationToLock = new SemaphoreSlim(1, 1);
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			IsRequireSignIn = isRequireSignIn;
			NowSignIn = false;

			HohoemaApp.OnSignout += __OnSignout;
			HohoemaApp.OnSignin += __OnSignin;

			_CompositeDisposable = new CompositeDisposable();
			_NavigatingCompositeDisposable = new CompositeDisposable();
			_UserSettingsCompositeDisposable = new CompositeDisposable();
		}

		private void __OnSignin()
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

		private void __OnSignout()
		{
			try
			{
				_SignStatusLock.Wait();

				if (IsRequireSignIn)
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


		


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			// PageManagerにナビゲーション動作を伝える
			PageManager.OnNavigated(e);


			base.OnNavigatedTo(e, viewModelState);

			HohoemaApp.OnResumed += _OnResumed;


			// 再生中動画のキャッシュクリアの除外条件をクリア
			if (HohoemaApp.MediaManager != null && HohoemaApp.MediaManager.Context != null)
			{
				HohoemaApp.MediaManager.Context.ClearPreventDeleteCacheOnPlayingVideo();
			}


			// サインインステータスチェック
			_NavigatedToTaskCancelToken = new CancellationTokenSource();

			_NavigatedToTask = __NavigatedToAsync(_NavigatedToTaskCancelToken.Token, e, viewModelState);

			if (!String.IsNullOrEmpty(_Title))
			{
				PageManager.PageTitle = _Title;
			}
			else
			{
				PageManager.PageTitle = PageManager.CurrentDefaultPageTitle();
			}


		}

		private async void _OnResumed()
		{
			await Task.Delay(300);

			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{	
				if (IsRequireSignIn)
				{
					if (!await CheckSignIn())
					{
						Mntone.Nico2.NiconicoSignInStatus result;
						try
						{
							result = await HohoemaApp.SignInWithPrimaryAccount();
						}
						catch
						{
							result = Mntone.Nico2.NiconicoSignInStatus.Failed;
						}

						if (result != Mntone.Nico2.NiconicoSignInStatus.Success)
						{
							// サインイン出来ない場合はログインページへ戻す
							PageManager.OpenPage(HohoemaPageType.Login);
							return;
						}

					}

					__OnSignin();
				}
				
					
				await OnResumed();
			});
		}

		protected virtual Task OnResumed()
		{
			return Task.CompletedTask;
		}


		private async Task __NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			using (var releaser = await _NavigationLock.LockAsync())
			{
				if (IsRequireSignIn)
				{
					if (!HohoemaApp.IsLoggedIn && !await CheckSignIn())
					{
						var result = await HohoemaApp.SignInWithPrimaryAccount();

						if (result != Mntone.Nico2.NiconicoSignInStatus.Success)
						{
							// サインイン出来ない場合はログインページへ戻す
							PageManager.OpenPage(HohoemaPageType.Login);
							return;
						}
					}

					__OnSignin();
				}

				if (HohoemaApp.MediaManager != null && HohoemaApp.MediaManager.Context != null)
				{
					await HohoemaApp.MediaManager.Context.ClearDurtyCachedNicoVideo();
				}

				await NavigatedToAsync(cancelToken, e, viewModelState);
			}
		}

		protected virtual Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			return Task.CompletedTask;
		}


		public override async void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			using (var releaser = await _NavigationLock.LockAsync())
			{
				_NavigatingCompositeDisposable?.Dispose();
				_NavigatingCompositeDisposable = new CompositeDisposable();

				if (!suspending)
				{
					HohoemaApp.OnResumed -= _OnResumed;
				}
				_NavigatedToTaskCancelToken?.Cancel();

				var task = _NavigatedToTask.WaitToCompelation();

				task.Wait();

				_NavigatedToTaskCancelToken?.Dispose();
				_NavigatedToTaskCancelToken = null;

				if (suspending)
				{
					await HohoemaApp.OnSuspending();
				}

				base.OnNavigatingFrom(e, viewModelState, suspending);
			}
		}

	
		protected void UpdateTitle(string title)
		{
			_Title = title;
			PageManager.UpdateTitle(title);
		}

		public async void Dispose()
		{
			using (var releaser = await _NavigationLock.LockAsync())
			{
				IsDisposed = true;

				if (IsRequireSignIn)
				{
					__OnSignout();
				}

				OnDispose();

				_CompositeDisposable?.Dispose();
				_UserSettingsCompositeDisposable?.Dispose();

				HohoemaApp.OnSignout -= __OnSignout;
				HohoemaApp.OnSignin -= __OnSignin;
			}
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
		protected CompositeDisposable _NavigatingCompositeDisposable { get; private set; }
		protected CompositeDisposable _UserSettingsCompositeDisposable { get; private set; }
	}
}

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
using Microsoft.Practices.Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaViewModelBase : ViewModelBase, IDisposable
	{

		static Util.AsyncLock _NavigationLock = new Util.AsyncLock();

        private Views.Service.AccountManagementDialogService _AccountManageDialogService;

        /// <summary>
        /// このページが利用可能になるアプリサービス状態を指定します。
        /// NavigatedToAsync内で変更されれば、その後アプリサービス状態ごとの
        /// 準備メソッドが呼び出されます。
        /// </summary>
        public HohoemaAppServiceLevel AvailableServiceLevel { get; private set; }


        public ReactiveProperty<bool> IsPageAvailable { get; private set; }

        public HohoemaViewModelBase(
            HohoemaApp hohoemaApp,
            PageManager pageManager, 
            bool canActivateBackgroundUpdate = true
            )
		{
            _AccountManageDialogService = App.Current.Container.Resolve<Views.Service.AccountManagementDialogService>();

            AvailableServiceLevel = HohoemaAppServiceLevel.Offline;

            _SignStatusLock = new SemaphoreSlim(1, 1);
			_NavigationToLock = new SemaphoreSlim(1, 1);
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			NowSignIn = false;

			CanActivateBackgroundUpdate = canActivateBackgroundUpdate;

            _CompositeDisposable = new CompositeDisposable();
			_NavigatingCompositeDisposable = new CompositeDisposable();
			_UserSettingsCompositeDisposable = new CompositeDisposable();

            IsPageAvailable = HohoemaApp.ObserveProperty(x => x.ServiceStatus)
                .Select(x => x >= AvailableServiceLevel)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

        }

		private void __OnSignin()
		{
			try
			{
				_SignStatusLock.Wait();

				if (!NowSignIn && HohoemaApp.IsLoggedIn)
				{
					NowSignIn = HohoemaApp.IsLoggedIn;

                    CallAppServiceLevelSignIn(_NavigatedToTaskCancelToken.Token);
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

				NowSignIn = false;

				_UserSettingsCompositeDisposable?.Dispose();
                _UserSettingsCompositeDisposable = new CompositeDisposable();
            }
            finally
			{
				_SignStatusLock.Release();
			}
		}

        protected virtual Task OnOffline(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnOnlineWithoutSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
		{
            return Task.CompletedTask;
		}

        
       
        private Task CallAppServiceLevelOffline(CancellationToken cancelToken)
        {
            if (AvailableServiceLevel <= HohoemaAppServiceLevel.Offline)
            {
                if (HohoemaApp.ServiceStatus <= HohoemaAppServiceLevel.Offline)
                {
                    return OnOffline(_NavigatingCompositeDisposable, cancelToken);
                }
            }

            return Task.CompletedTask;
        }
        private Task CallAppServiceLevelOnlineWithoutLoggedIn(CancellationToken cancelToken)
        {
            if (AvailableServiceLevel <= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
            {
                if (HohoemaApp.ServiceStatus <= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
                {
                    return OnOnlineWithoutSignIn(_NavigatingCompositeDisposable, cancelToken);
                }
            }

            return Task.CompletedTask;
        }

        private Task CallAppServiceLevelSignIn(CancellationToken cancelToken)
        {
            if (AvailableServiceLevel <= HohoemaAppServiceLevel.LoggedIn)
            {
                if (HohoemaApp.ServiceStatus <= HohoemaAppServiceLevel.LoggedIn)
                {
                    return OnSignIn(_UserSettingsCompositeDisposable, cancelToken);
                }
            }

            return Task.CompletedTask;
        }


		protected async Task<bool> CheckSignIn()
		{
            if (IsRequireSignIn)
            {
                var isSignIn = await HohoemaApp.CheckSignedInStatus() == Mntone.Nico2.NiconicoSignInStatus.Success;
                if (!HohoemaApp.IsLoggedIn && !isSignIn)
                {
                    var result = await HohoemaApp.SignInWithPrimaryAccount();

                    if (result == Mntone.Nico2.NiconicoSignInStatus.Failed)
                    {
                        // サインイン出来ない場合はアカウント画面を表示する
                        await _AccountManageDialogService.ShowChangeAccountDialogAsync();
                    }
                }

                __OnSignin();
            }

            return HohoemaApp.IsLoggedIn;
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
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
                await CallAppServiceLevelOffline(_NavigatedToTaskCancelToken.Token);

                await CallAppServiceLevelOnlineWithoutLoggedIn(_NavigatedToTaskCancelToken.Token);

                await CheckSignIn();
					
				await OnResumed();

				// BG更新処理を再開
				if (CanActivateBackgroundUpdate)
				{
					HohoemaApp.BackgroundUpdater.Activate();
				}
				else
				{
					HohoemaApp.BackgroundUpdater.Deactivate();
				}
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
                if (HohoemaApp.MediaManager != null && HohoemaApp.MediaManager.Context != null)
				{
					await HohoemaApp.MediaManager.Context.ClearDurtyCachedNicoVideo();
				}

				// Note: BGUpdateの再有効化はナビゲーション処理より前で行う
				// ナビゲーション処理内でBGUpdate待ちをした場合に、デッドロックする可能性がでる

				// BG更新処理を再開
				if (CanActivateBackgroundUpdate)
				{
					HohoemaApp.BackgroundUpdater.Activate();
				}
				else
				{
					HohoemaApp.BackgroundUpdater.Deactivate();
				}

				await NavigatedToAsync(cancelToken, e, viewModelState);



                await CallAppServiceLevelOffline(_NavigatedToTaskCancelToken.Token);

                await CallAppServiceLevelOnlineWithoutLoggedIn(_NavigatedToTaskCancelToken.Token);

                await CheckSignIn();

                if (IsRequireSignIn)
                {
                    HohoemaApp.OnSignout += __OnSignout;
                    HohoemaApp.OnSignin += __OnSignin;
                }
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

				await _NavigatedToTask.WaitToCompelation();

				_NavigatedToTaskCancelToken?.Dispose();
				_NavigatedToTaskCancelToken = null;

				if (suspending)
				{
					await HohoemaApp.OnSuspending();
				}
				
				base.OnNavigatingFrom(e, viewModelState, suspending);
			}
		}

        protected void ChangeRequireServiceLevel(HohoemaAppServiceLevel serviceLevel)
        {
            AvailableServiceLevel = serviceLevel;

            Debug.WriteLine(_Title + " require service level: " + AvailableServiceLevel.ToString());
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


        public bool IsRequireSignIn => AvailableServiceLevel == HohoemaAppServiceLevel.LoggedIn;

        public bool NowSignIn { get; private set; }

		public bool CanActivateBackgroundUpdate { get; private set; }

		private string _Title;

		public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		protected CompositeDisposable _CompositeDisposable { get; private set; }
		protected CompositeDisposable _NavigatingCompositeDisposable { get; private set; }
		protected CompositeDisposable _UserSettingsCompositeDisposable { get; private set; }
	}
}

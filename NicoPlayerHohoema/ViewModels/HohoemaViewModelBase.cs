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
using NicoPlayerHohoema.Helpers;
using System.Runtime.InteropServices.WindowsRuntime;
using WinRTXamlToolkit.Async;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Microsoft.Practices.Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Windows.UI.Core;
using NicoPlayerHohoema.Views.Service;
using Mntone.Nico2;
using System.Reactive.Concurrency;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaViewModelBase : ViewModelBase, IDisposable
	{
        private SynchronizationContextScheduler _CurrentWindowContextScheduler;
        public SynchronizationContextScheduler CurrentWindowContextScheduler
        {
            get
            {
                return _CurrentWindowContextScheduler
                    ?? (_CurrentWindowContextScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
            }
        }

        // 代替キャンセル動作の管理ID
        const string PlayerFillModeBackNavigationCancel = "fill_mode_cancel";

        static Helpers.AsyncLock _NavigationLock = new Helpers.AsyncLock();

        /// <summary>
        /// このページが利用可能になるアプリサービス状態を指定します。
        /// NavigatedToAsync内で変更されれば、その後アプリサービス状態ごとの
        /// 準備メソッドが呼び出されます。
        /// </summary>
        public HohoemaAppServiceLevel PageRequireServiceLevel { get; private set; }


        public ReactiveProperty<bool> IsPageAvailable { get; private set; }
        public bool UseDefaultPageTitle { get; }

        public HohoemaViewModelBase(
            HohoemaApp hohoemaApp,
            PageManager pageManager, 
            bool useDefaultPageTitle = true
            )
		{
            PageRequireServiceLevel = HohoemaAppServiceLevel.Offline;

            _SignStatusLock = new SemaphoreSlim(1, 1);
			_NavigationToLock = new SemaphoreSlim(1, 1);
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			NowSignIn = false;

            UseDefaultPageTitle = useDefaultPageTitle;

            _CompositeDisposable = new CompositeDisposable();
			_NavigatingCompositeDisposable = new CompositeDisposable();

            IsPageAvailable = HohoemaApp.ObserveProperty(x => x.ServiceStatus)
                .Select(x => x >= PageRequireServiceLevel)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

            if (Helpers.DeviceTypeHelper.IsXbox)
            {
                IsForceTVModeEnable = new ReactiveProperty<bool>(true);
            }
            else
            {
                IsForceTVModeEnable = HohoemaApp.UserSettings.AppearanceSettings.ObserveProperty(x => x.IsForceTVModeEnable)
                    .ToReactiveProperty();
            }

            SubstitutionBackNavigation = new Dictionary<string, Func<bool>>();
            
        }

        

		private void __OnSignin()
		{
			try
			{
				_SignStatusLock.Wait();

				if (!NowSignIn && HohoemaApp.IsLoggedIn)
				{
					NowSignIn = HohoemaApp.IsLoggedIn;

                    CallAppServiceLevelSignIn(_NavigatedToTaskCancelToken?.Token ?? CancellationToken.None);
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

                if (IsRequireSignIn)
                {
                    OnSignOut().ConfigureAwait(false);
                }
            }
            finally
			{
				_SignStatusLock.Release();
			}
		}


        /// <summary>
        /// ユーザーがニコニコサービスからログアウトした場合に呼ばれます。（アプリ終了時を除く）
        /// ログインが必要なサービスレベルを設定していた場合に呼ばれます。
        /// </summary>
        /// <returns></returns>
        protected virtual Task OnSignOut()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnOffline(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnOnlineWithoutSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnDisconnected()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
		{
            return Task.CompletedTask;
		}

        protected virtual Task OnFailedSignIn()
        {
            PageManager.OpenPage(HohoemaPageType.Login);

            return Task.CompletedTask;
        }


        private Task CallAppServiceLevelOffline(CancellationToken cancelToken)
        {
//            if (AvailableServiceLevel >= HohoemaAppServiceLevel.OnlineButServiceUnavailable)
            {
 //               if (HohoemaApp.ServiceStatus >= HohoemaAppServiceLevel.OnlineButServiceUnavailable)
                {
                    return OnOffline(_NavigatingCompositeDisposable, cancelToken);
                }
            }

            //return Task.CompletedTask;
        }
        private Task CallAppServiceLevelOnlineWithoutLoggedIn(CancellationToken cancelToken)
        {
            if (PageRequireServiceLevel >= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
            {
                if (HohoemaApp.ServiceStatus >= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
                {
                    return OnOnlineWithoutSignIn(_NavigatingCompositeDisposable, cancelToken);
                }
                else
                {
                    return OnDisconnected();
                }
            }

            return Task.CompletedTask;
        }

        private Task CallAppServiceLevelSignIn(CancellationToken cancelToken)
        {
            if (PageRequireServiceLevel >= HohoemaAppServiceLevel.LoggedIn)
            {
                if (HohoemaApp.ServiceStatus >= HohoemaAppServiceLevel.LoggedIn)
                {
                    return OnSignIn(_NavigatingCompositeDisposable, cancelToken);
                }
                else
                {
                    return OnFailedSignIn();
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
                    if (!AccountManager.HasPrimaryAccount())
                    {
                        return false;
                    }
                    else
                    {
                        var result = await HohoemaApp.SignInWithPrimaryAccount();

                        if (result == Mntone.Nico2.NiconicoSignInStatus.Failed)
                        {
                            return false;
                        }
                    }
                }
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
								PageManager.OpenStartupPage();
                            }
						}));
			}
		}





        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            // PageManagerにナビゲーション動作を伝える
            PageManager.OnNavigated(e);

            if (UseDefaultPageTitle)
            {
                Title = PageManager.CurrentDefaultPageTitle();                
            }

            base.OnNavigatedTo(e, viewModelState);

            //            HohoemaApp.OnResumed += _OnResumed;

            
            try
            {
                // サインインステータスチェック
                _NavigatedToTaskCancelToken = new CancellationTokenSource();

                _NavigatedToTask = __NavigatedToAsync(_NavigatedToTaskCancelToken.Token, e, viewModelState);
            }
            catch
            {

            }

            if (CoreApplication.GetCurrentView().IsMain)
            {
                if (!String.IsNullOrEmpty(Title))
                {
                    PageManager.PageTitle = Title;
                }
                else
                {
                    PageManager.PageTitle = PageManager.CurrentDefaultPageTitle();
                }
            }
        }

        private async void _OnResumed()
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
                CancellationToken token = _NavigatedToTaskCancelToken?.Token ?? CancellationToken.None;
                await CallAppServiceLevelOffline(token);

                await CallAppServiceLevelOnlineWithoutLoggedIn(token);

                await CheckSignIn();
					
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
                // Note: BGUpdateの再有効化はナビゲーション処理より前で行う
                // ナビゲーション処理内でBGUpdate待ちをした場合に、デッドロックする可能性がでる

                await NavigatedToAsync(cancelToken, e, viewModelState);

                cancelToken.ThrowIfCancellationRequested();

                await CallAppServiceLevelOffline(cancelToken);

                cancelToken.ThrowIfCancellationRequested();

                await CallAppServiceLevelOnlineWithoutLoggedIn(cancelToken);

                cancelToken.ThrowIfCancellationRequested();

                if (await CheckSignIn())
                {
                    __OnSignin();
                }

                if (IsRequireSignIn)
                {
                    HohoemaApp.OnSignout += __OnSignout;
                    HohoemaApp.OnSignin += __OnSignin;
                }

                if (string.IsNullOrEmpty(Title))
                {
                    UpdateTitle(PageManager.CurrentDefaultPageTitle());
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
                // バックナビゲーションが発生した時、
                // かつ、代替バックナビゲーション動作が設定されている場合に、
                // バックナビゲーションをキャンセルします。
                if (!suspending
                    && e.NavigationMode == NavigationMode.Back
                    && SubstitutionBackNavigation.Count > 0
                    )
                {
                    e.Cancel = true;
                    return;
                }

                _NavigatedToTaskCancelToken?.Cancel();

                await _NavigatedToTask.WaitToCompelation();

                _NavigatedToTaskCancelToken?.Dispose();
                _NavigatedToTaskCancelToken = null;


                try
                {
                    OnHohoemaNavigatingFrom(e, viewModelState, suspending);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                _NavigatingCompositeDisposable?.Dispose();
                _NavigatingCompositeDisposable = new CompositeDisposable();

                if (!suspending)
                {
                    HohoemaApp.OnResumed -= _OnResumed;
                }

                base.OnNavigatingFrom(e, viewModelState, suspending);
            }
		}

        protected virtual void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {

        }

        protected void ChangeRequireServiceLevel(HohoemaAppServiceLevel serviceLevel)
        {
            PageRequireServiceLevel = serviceLevel;

            Debug.WriteLine(Title + " require service level: " + PageRequireServiceLevel.ToString());
        }

	
		protected void UpdateTitle(string title)
		{
			Title = title;
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


        public bool IsRequireSignIn => PageRequireServiceLevel == HohoemaAppServiceLevel.LoggedIn;

        public bool NowSignIn { get; private set; }

		private string _Title;
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        
        public ReactiveProperty<bool> IsForceTVModeEnable { get; private set; }


        public static Dictionary<string, Func<bool>> SubstitutionBackNavigation { get; private set; } = new Dictionary<string, Func<bool>>();


        public HohoemaApp HohoemaApp { get; private set; }
		public PageManager PageManager { get; private set; }

		protected CompositeDisposable _CompositeDisposable { get; private set; }
		protected CompositeDisposable _NavigatingCompositeDisposable { get; private set; }
	}
}

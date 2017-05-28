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
using Windows.UI.Core;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaViewModelBase : ViewModelBase, IDisposable
	{

		static Util.AsyncLock _NavigationLock = new Util.AsyncLock();

        /// <summary>
        /// このページが利用可能になるアプリサービス状態を指定します。
        /// NavigatedToAsync内で変更されれば、その後アプリサービス状態ごとの
        /// 準備メソッドが呼び出されます。
        /// </summary>
        public HohoemaAppServiceLevel AvailableServiceLevel { get; private set; }


        public ReactiveProperty<bool> IsPageAvailable { get; private set; }
        public bool UseDefaultPageTitle { get; }

        public HohoemaViewModelBase(
            HohoemaApp hohoemaApp,
            PageManager pageManager, 
            bool canActivateBackgroundUpdate = true,
            bool useDefaultPageTitle = true
            )
		{
            AvailableServiceLevel = HohoemaAppServiceLevel.Offline;

            _SignStatusLock = new SemaphoreSlim(1, 1);
			_NavigationToLock = new SemaphoreSlim(1, 1);
			HohoemaApp = hohoemaApp;
			PageManager = pageManager;

			NowSignIn = false;

			CanActivateBackgroundUpdate = canActivateBackgroundUpdate;
            UseDefaultPageTitle = useDefaultPageTitle;

            _CompositeDisposable = new CompositeDisposable();
			_NavigatingCompositeDisposable = new CompositeDisposable();
			_UserSettingsCompositeDisposable = new CompositeDisposable();

            IsPageAvailable = HohoemaApp.ObserveProperty(x => x.ServiceStatus)
                .Select(x => x >= AvailableServiceLevel)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

            if (Util.DeviceTypeHelper.IsXbox)
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
            if (AvailableServiceLevel >= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
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
            if (AvailableServiceLevel >= HohoemaAppServiceLevel.LoggedIn)
            {
                if (HohoemaApp.ServiceStatus >= HohoemaAppServiceLevel.LoggedIn)
                {
                    return OnSignIn(_UserSettingsCompositeDisposable, cancelToken);
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
								PageManager.OpenPage(HohoemaPageType.Portal);
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

			HohoemaApp.OnResumed += _OnResumed;

            // TODO: プレイヤーを別ウィンドウにしている場合に、プレイヤーの表示状態変更を抑制する
            // プレイヤーがフィル表示している時にバックキーのアクションを再定義する
            Observable.CombineLatest(
                HohoemaApp.Playlist.ObserveProperty(x => x.IsPlayerFloatingModeEnable).Select(x => !x),
                HohoemaApp.Playlist.ObserveProperty(x => x.IsDisplayPlayer)
                )
                .Select(x => x.All(y => y))
                .Subscribe(isBackNavigationClosePlayer =>
                {
                    const string PlayerFillModeBackNavigationCancel = "fill_mode_cancel";
                    if (isBackNavigationClosePlayer)
                    {
                        AddSubsitutionBackNavigateAction(PlayerFillModeBackNavigationCancel, () =>
                        {
                            // Bボタンによる動画プレイヤーを閉じる動作を一切受け付けない
                            HohoemaApp.Playlist.IsDisplayPlayerControlUI = !HohoemaApp.Playlist.IsDisplayPlayerControlUI;
                            return false;
                        });
                    }
                    else
                    {
                        RemoveSubsitutionBackNavigateAction(PlayerFillModeBackNavigationCancel);
                    }
                })
                .AddTo(_NavigatingCompositeDisposable);

            // サインインステータスチェック
            _NavigatedToTaskCancelToken = new CancellationTokenSource();

			_NavigatedToTask = __NavigatedToAsync(_NavigatedToTaskCancelToken.Token, e, viewModelState);

			if (!String.IsNullOrEmpty(Title))
			{
				PageManager.PageTitle = Title;
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
                CancellationToken token = _NavigatedToTaskCancelToken?.Token ?? CancellationToken.None;
                await CallAppServiceLevelOffline(token);

                await CallAppServiceLevelOnlineWithoutLoggedIn(token);

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

        protected virtual void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {

        }

        protected void ChangeRequireServiceLevel(HohoemaAppServiceLevel serviceLevel)
        {
            AvailableServiceLevel = serviceLevel;

            Debug.WriteLine(Title + " require service level: " + AvailableServiceLevel.ToString());
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
				_UserSettingsCompositeDisposable?.Dispose();

				HohoemaApp.OnSignout -= __OnSignout;
				HohoemaApp.OnSignin -= __OnSignin;
			}
		}



        protected void AddSubsitutionBackNavigateAction(string id, Func<bool> action)
        {
            if (!SubstitutionBackNavigation.ContainsKey(id))
            {
                SubstitutionBackNavigation.Add(id, action);

                var nav = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
                nav.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
                nav.BackRequested += Nav_BackRequested;
            }
        }

        private void Nav_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (SubstitutionBackNavigation.Count > 0)
            {
                var substitutionBackNavPair = SubstitutionBackNavigation.Last();
                var action = substitutionBackNavPair.Value;
                
                if (SubstitutionBackNavigation.Count == 0)
                {
                    var nav = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
                    nav.BackRequested -= Nav_BackRequested;

                    // バックナビゲーションが出来ない場合にBackButtonを非表示に
                    var pageManager = App.Current.Container.Resolve<PageManager>();
                    if (!pageManager.NavigationService.CanGoBack())
                    {
                        nav.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
                    }
                }

                if (action?.Invoke() ?? false)
                {
                    SubstitutionBackNavigation.Remove(substitutionBackNavPair.Key);
                }

                e.Handled = true;
            }
            
        }

        protected bool RemoveSubsitutionBackNavigateAction(string id)
        {
            if (SubstitutionBackNavigation.ContainsKey(id))
            {
                if (SubstitutionBackNavigation.Count == 1)
                {
                    var nav = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
                    nav.BackRequested -= Nav_BackRequested;

                    // バックナビゲーションが出来ない場合にBackButtonを非表示に
                    var pageManager = App.Current.Container.Resolve<PageManager>();
                    if (!pageManager.NavigationService.CanGoBack())
                    {
                        nav.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
                    }
                }

                return SubstitutionBackNavigation.Remove(id);
            }
            else
            {
                return false;
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
		protected CompositeDisposable _UserSettingsCompositeDisposable { get; private set; }
	}
}

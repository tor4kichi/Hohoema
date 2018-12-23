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
using NicoPlayerHohoema.Models.Helpers;
using System.Runtime.InteropServices.WindowsRuntime;
using WinRTXamlToolkit.Async;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Microsoft.Practices.Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Windows.UI.Core;
using NicoPlayerHohoema.Services;
using Mntone.Nico2;
using System.Reactive.Concurrency;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaViewModelBase : ViewModelBase, IDisposable
	{

        public HohoemaViewModelBase(
            PageManager pageManager
            )
        {
            PageManager = pageManager;

            _CompositeDisposable = new CompositeDisposable();
            _NavigatingCompositeDisposable = new CompositeDisposable();
        }



        private SynchronizationContextScheduler _CurrentWindowContextScheduler;
        public SynchronizationContextScheduler CurrentWindowContextScheduler
        {
            get
            {
                return _CurrentWindowContextScheduler
                    ?? (_CurrentWindowContextScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
            }
        }


        public PageManager PageManager { get; }

        protected CompositeDisposable _CompositeDisposable { get; private set; }
        protected CompositeDisposable _NavigatingCompositeDisposable { get; private set; }



        static Models.Helpers.AsyncLock _NavigationLock = new Models.Helpers.AsyncLock();
        CancellationTokenSource _NavigatedToTaskCancelToken;
        Task _NavigatedToTask;

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
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
						}));
			}
		}


        #region IDisposable



        public bool IsDisposed { get; private set; }


        public async void Dispose()
        {
            using (var releaser = await _NavigationLock.LockAsync())
            {
                IsDisposed = true;

                OnDispose();

                _CompositeDisposable?.Dispose();
            }
        }

        protected virtual void OnDispose() { }

        



        #endregion


        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            // PageManagerにナビゲーション動作を伝える
            PageManager.OnNavigated(e);

            if (!IsPageNameResolveOnPostNavigatedToAsync)
            {
                if (false == (this is VideoPlayerPageViewModel || this is LivePlayerPageViewModel))
                {
                    try
                    {
                        Title = ResolvePageName();
                    }
                    catch
                    {
                        Title = PageManager.CurrentDefaultPageTitle();
                    }

                }
            }

            base.OnNavigatedTo(e, viewModelState);

            
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
                if (false == (this is VideoPlayerPageViewModel || this is LivePlayerPageViewModel))
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

                if (false == (this is VideoPlayerPageViewModel || this is LivePlayerPageViewModel))
                {
                    if (IsPageNameResolveOnPostNavigatedToAsync)
                    {
                        PageManager.PageTitle = ResolvePageName();
                        Title = PageManager.PageTitle;
                    }
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
                    && PageManager.PreventBackNavigation
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

                base.OnNavigatingFrom(e, viewModelState, suspending);
            }
		}

        protected virtual void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
        }

        public bool IsPageNameResolveOnPostNavigatedToAsync { get; protected set; } = false;
        protected virtual string ResolvePageName()
        {
            return PageManager.CurrentDefaultPageTitle();
        }

	}
}

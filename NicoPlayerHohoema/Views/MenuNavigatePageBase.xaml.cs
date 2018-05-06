using Microsoft.Toolkit.Uwp.UI.Animations;
using NicoPlayerHohoema.Helpers;
using Prism.Events;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class MenuNavigatePageBase : ContentControl
	{
        CoreDispatcher _UIDispatcher;
        public MenuNavigatePageBase()
		{
			this.InitializeComponent();

            this.Loading += MenuNavigatePageBase_Loading;
            this.Loaded += MenuNavigatePageBase_Loaded;
        }

       
        private void MenuNavigatePageBase_Loading(FrameworkElement sender, object args)
		{
			ForceChangeChildDataContext();

        }

        private void MenuNavigatePageBase_Loaded(object sender, RoutedEventArgs e)
        {
            _UIDispatcher = Dispatcher;

            
        }
        

        private void ForceChangeChildDataContext()
		{
			if (Parent is FrameworkElement && Content is FrameworkElement)
			{
				var depContent = Content as FrameworkElement;
				depContent.DataContext = (Parent as FrameworkElement).DataContext;
			}
		}

        public Frame Frame { get; private set; }

        protected override void OnApplyTemplate()
        {
            Frame = GetTemplateChild("PlayerFrame") as Frame;

            {
                var frameFacade = new FrameFacadeAdapter(Frame);

                var sessionStateService = new SessionStateService();

                var ns = new FrameNavigationService(frameFacade
                    , (pageToken) =>
                    {
                        if (pageToken == nameof(Views.VideoPlayerPage))
                        {
                            return typeof(Views.VideoPlayerPage);
                        }
                        else if (pageToken == nameof(Views.LivePlayerPage))
                        {
                            return typeof(Views.LivePlayerPage);
                        }
                        else
                        {
                            return typeof(Views.BlankPage);
                        }
                    }, sessionStateService);

                (DataContext as ViewModels.MenuNavigatePageBaseViewModel).SetNavigationService(ns);
            }

            // タイトルバーのハンドルできる範囲を自前で指定する
            // バックボタンのカスタマイズ対応のため
            // もしかしてモバイルやXboxOneで例外が出てクラッシュするのが怖いので
            // 例外を握りつぶしておく
            try
            {
                Window.Current.SetTitleBar(GetTemplateChild("DraggableContent") as UIElement);
            }
            catch { }


            var subpageContentControl = GetTemplateChild("SubPageContentControl") as ContentControl;
            if (subpageContentControl != null)
            {
                subpageContentControl.ObserveDependencyProperty(ContentControl.ContentProperty)
                    .Subscribe(x => 
                    {
                        if (subpageContentControl.Content == null)
                        {
                            ShowMenuMainContent();
                        }
                        else
                        {
                            ShowMenuSubContent();
                        }
                    });
            }

            base.OnApplyTemplate();
        }





        AsyncLock _MenuContentToggleLock = new AsyncLock();

        static readonly double MenuMainSubContentToggleAnimationDuration = 175;
        public async void ShowMenuMainContent()
        {
            var showTarget = GetTemplateChild("MenuMainPageContent") as FrameworkElement;
            var hideTarget = GetTemplateChild("MenuSubPageContent") as FrameworkElement;

            using (var releaser = await _MenuContentToggleLock.LockAsync())
            {
                showTarget.Visibility = Visibility.Visible;
                hideTarget.IsHitTestVisible = false;
                await Task.WhenAll(
                    showTarget.Fade(1.0f, MenuMainSubContentToggleAnimationDuration).Offset(0, duration: MenuMainSubContentToggleAnimationDuration).StartAsync(),
                    hideTarget.Fade(0.0f, MenuMainSubContentToggleAnimationDuration).Offset(100, duration: MenuMainSubContentToggleAnimationDuration).StartAsync()
                    );
                showTarget.IsHitTestVisible = true;
                hideTarget.Visibility = Visibility.Collapsed;
            }
        }

        public async void ShowMenuSubContent()
        {
            var hideTarget = GetTemplateChild("MenuMainPageContent") as FrameworkElement;
            var showTarget = GetTemplateChild("MenuSubPageContent") as FrameworkElement;

            using (var releaser = await _MenuContentToggleLock.LockAsync())
            {
                showTarget.Visibility = Visibility.Visible;
                hideTarget.IsHitTestVisible = false;
                await Task.WhenAll(
                    showTarget.Fade(1.0f, MenuMainSubContentToggleAnimationDuration).Offset(0, duration:MenuMainSubContentToggleAnimationDuration).StartAsync(),
                    hideTarget.Fade(0.0f, MenuMainSubContentToggleAnimationDuration).Offset(-100, duration: MenuMainSubContentToggleAnimationDuration).StartAsync()
                    );
                showTarget.IsHitTestVisible = true;
                hideTarget.Visibility = Visibility.Collapsed;
            }
        }


        /// <summary>
        /// 検索を実行した際、モバイルやXboxOneのときは自動でメニューを閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SearchTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var splitView = GetTemplateChild("ContentSplitView") as SplitView;
            if (splitView != null)
            {
                if (splitView.DisplayMode == SplitViewDisplayMode.Overlay)
                {
                    splitView.IsPaneOpen = false;
                }
            }
        }
    }
}

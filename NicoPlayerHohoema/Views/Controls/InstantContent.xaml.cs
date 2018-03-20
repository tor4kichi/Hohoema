using Microsoft.Toolkit.Uwp.UI.Animations;
using NicoPlayerHohoema.Helpers;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.Controls
{
    public sealed partial class InstantContent : UserControl
    {

        public static string ContentDisplayedVisualStateName = @"ContentDisplayed";
        public static string ContentKeepShowingVisualStateName = @"ContentKeepShowing";
        public static string ContentHiddenVisualStateName = @"ContentHidden";


        string CurrentStateName = ContentHiddenVisualStateName;
        bool _CurrentKeepShowing => CurrentStateName == ContentKeepShowingVisualStateName;

        public static readonly DependencyProperty DisplayContentProperty =
            DependencyProperty.Register(nameof(DisplayContent)
                    , typeof(object)
                    , typeof(InstantContent)
                    , new PropertyMetadata(default(object))
                );
                

        public object DisplayContent
        {
            get { return (object)GetValue(DisplayContentProperty); }
            set { SetValue(DisplayContentProperty, value); }
        }



        public static readonly DependencyProperty DisplayContentTemplateProperty =
            DependencyProperty.Register(nameof(DisplayContentTemplate)
                    , typeof(DataTemplate)
                    , typeof(InstantContent)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate DisplayContentTemplate
        {
            get { return (DataTemplate)GetValue(DisplayContentTemplateProperty); }
            set { SetValue(DisplayContentTemplateProperty, value); }
        }


        public static readonly DependencyProperty DisplayDurationProperty =
            DependencyProperty.Register(nameof(DisplayDuration)
                    , typeof(TimeSpan)
                    , typeof(InstantContent)
                    , new PropertyMetadata(TimeSpan.FromSeconds(2.5)));


        public TimeSpan DisplayDuration
        {
            get { return (TimeSpan)GetValue(DisplayDurationProperty); }
            set { SetValue(DisplayDurationProperty, value); }
        }


        public static readonly DependencyProperty IsAutoHideEnabledProperty =
           DependencyProperty.Register(nameof(IsAutoHideEnabled)
                   , typeof(bool)
                   , typeof(InstantContent)
                   , new PropertyMetadata(true)
               );


        public bool IsAutoHideEnabled
        {
            get { return (bool)GetValue(IsAutoHideEnabledProperty); }
            set { SetValue(IsAutoHideEnabledProperty, value); }
        }

        public InstantContent()
        {
            this.InitializeComponent();

            Loaded += InstantContent_Loaded;
            Unloaded += InstantContent_Unloaded;
        }

        AsyncLock _AnimLock = new AsyncLock();
        AnimationSet _PrevFadeAnimation;

        CompositeDisposable _CompositeDisposable;

        private void InstantContent_Loaded(object sender, RoutedEventArgs e)
        {
            _CompositeDisposable = new CompositeDisposable();
            this.ObserveDependencyProperty(IsAutoHideEnabledProperty)
                .Subscribe(_ =>
                {
                    ResetAnimation();
                })
                .AddTo(_CompositeDisposable);

            this.ObserveDependencyProperty(DisplayContentProperty)
                .Subscribe(_ =>
                {
                    ResetAnimation();
                })
                .AddTo(_CompositeDisposable);
        }

        private void InstantContent_Unloaded(object sender, RoutedEventArgs e)
        {
            _CompositeDisposable?.Dispose();
        }

        async void ResetAnimation()
        {            
            using (var releaser = await _AnimLock.LockAsync())
            {
                if (_PrevFadeAnimation != null)
                {
                    var prevAnimState = _PrevFadeAnimation.State;
                    _PrevFadeAnimation?.Dispose();
                    if (prevAnimState == AnimationSetState.Running)
                    {
                        // 前アニメーションが実行中だった場合は終わるまで待機
                        // （ここでは横着して50ms止めるだけ）
                        await Task.Delay(50);
                    }
                }

                if (DisplayContent != null)
                {
                    _PrevFadeAnimation = ContentContainer
                            .Fade(1.0f, 100);

                    if (IsAutoHideEnabled)
                    {
                        _PrevFadeAnimation = _PrevFadeAnimation.Then()
                            .Fade(0.0f, 100, delay: DisplayDuration.TotalMilliseconds);
                    }
                }
                else
                {
                    _PrevFadeAnimation = ContentContainer
                        .Fade(0.0f, 100);
                }

                _PrevFadeAnimation?.StartAsync().ConfigureAwait(false);
            }
        }
    }
}

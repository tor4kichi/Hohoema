using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Reactive.Bindings.Binding;
using Reactive.Bindings.Extensions;
using System.Threading.Tasks;
// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace NicoPlayerHohoema.Views.Controls
{
    public sealed partial class TransientContainer : Control
    {
        public TransientContainer()
        {
            this.DefaultStyleKey = typeof(TransientContainer);

            Loaded += TransientContainer_Loaded;
            Unloaded += TransientContainer_Unloaded;
        }

        Models.Helpers.AsyncLock _AnimLock = new Models.Helpers.AsyncLock();
        AnimationSet _PrevFadeAnimation;

        CompositeDisposable _CompositeDisposable;

        private void TransientContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _CompositeDisposable = new CompositeDisposable();
            this.ObserveDependencyProperty(IsAutoHideEnabledProperty)
                .Subscribe(_ =>
                {
                    ResetAnimation();
                })
                .AddTo(_CompositeDisposable);

            this.ObserveDependencyProperty(ContentProperty)
                .Subscribe(_ =>
                {
                    ResetAnimation();
                })
                .AddTo(_CompositeDisposable);

            (GetTemplateChild("ContentContainer") as FrameworkElement).ObserveDependencyProperty(OpacityProperty)
                .Subscribe(_ =>
                {
                    var container = (GetTemplateChild("ContentContainer") as FrameworkElement);
                    container.Visibility = container.Opacity == 0 ? Visibility.Collapsed : Visibility.Visible;
                })
                .AddTo(_CompositeDisposable);
        }

        private void TransientContainer_Unloaded(object sender, RoutedEventArgs e)
        {
            _CompositeDisposable?.Dispose();
        }

        async void ResetAnimation()
        {
            var container = GetTemplateChild("ContentContainer") as FrameworkElement;
            if (container == null) { return; }

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

                if (Content != null)
                {
                    _PrevFadeAnimation = container
                            .Fade(1.0f, 100);

                    if (IsAutoHideEnabled)
                    {
                        _PrevFadeAnimation = _PrevFadeAnimation.Then()
                            .Fade(0.0f, 100, delay: DisplayDuration.TotalMilliseconds);
                    }
                }
                else
                {
                    _PrevFadeAnimation = container
                        .Fade(0.0f, 100);
                }

                _PrevFadeAnimation?.StartAsync().ConfigureAwait(false);
            }
        }
    }
}

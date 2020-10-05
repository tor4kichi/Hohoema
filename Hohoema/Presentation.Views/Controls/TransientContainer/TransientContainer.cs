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

namespace Hohoema.Presentation.Views.Controls
{
    public sealed partial class TransientContainer : Control
    {
        public TransientContainer()
        {
            this.DefaultStyleKey = typeof(TransientContainer);

            Loaded += TransientContainer_Loaded;
            Unloaded += TransientContainer_Unloaded;
        }

        Models.Domain.Helpers.AsyncLock _AnimLock = new Models.Domain.Helpers.AsyncLock();
        AnimationSet _FadeInAnimation;
        AnimationSet _FadeOutAnimation;

        CompositeDisposable _CompositeDisposable;

        private void TransientContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _CompositeDisposable = new CompositeDisposable();
            this.ObserveDependencyProperty(IsAutoHideEnabledProperty)
                .Subscribe(_ =>
                {
                    _FadeInAnimation?.Dispose();
                    _FadeInAnimation = null;
                    ResetAnimation();
                })
                .AddTo(_CompositeDisposable);

            this.ObserveDependencyProperty(ContentProperty)
                .Subscribe(_ =>
                {
                    ResetAnimation();
                })
                .AddTo(_CompositeDisposable);

            var container = GetTemplateChild("ContentContainer") as FrameworkElement;
            container.Fade(0.0f, 0).Start();
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
                if (Content != null)
                {
                    _FadeOutAnimation?.Stop();
                    _FadeInAnimation?.Stop();

                    _FadeInAnimation = container
                            .Fade(1.0f, 100);

                    if (IsAutoHideEnabled)
                    {
                        _FadeInAnimation = _FadeInAnimation.Then()
                            .Fade(0.0f, 100, delay: DisplayDuration.TotalMilliseconds);
                    }

                    _FadeInAnimation.Start();
                }
                else
                {
                    _FadeInAnimation?.Stop();
                    _FadeOutAnimation?.Stop();

                    _FadeOutAnimation = container
                        .Fade(0.0f, 100);

                    _FadeOutAnimation.Start();
                }

            }
        }
    }
}

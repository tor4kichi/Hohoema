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
using System.Threading;
using Uno.Threading;
using Windows.System;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Hohoema.Presentation.Views.Controls
{
    public sealed partial class TransientContainer : Control
    {


        public TimeSpan FadeInDuration
        {
            get { return (TimeSpan)GetValue(FadeInDurationProperty); }
            set { SetValue(FadeInDurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FadeInDuration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FadeInDurationProperty =
            DependencyProperty.Register("FadeInDuration", typeof(TimeSpan), typeof(TransientContainer), new PropertyMetadata(TimeSpan.FromSeconds(0.1)));



        public TimeSpan FadeOutDuration
        {
            get { return (TimeSpan)GetValue(FadeOutDurationProperty); }
            set { SetValue(FadeOutDurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FadeOutDuration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FadeOutDurationProperty =
            DependencyProperty.Register("FadeOutDuration", typeof(TimeSpan), typeof(TransientContainer), new PropertyMetadata(TimeSpan.FromSeconds(0.1)));




        public TransientContainer()
        {
            this.DefaultStyleKey = typeof(TransientContainer);

            Loaded += TransientContainer_Loaded;
            Unloaded += TransientContainer_Unloaded;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _container = GetTemplateChild("ContentContainer") as FrameworkElement;
            AnimationBuilder.Create()
                       .Opacity()
                       .NormalizedKeyFrames(b => b
                           .KeyFrame(0, 0.0, easingType: EasingType.Linear)
                           )
                       .Start(_container);
        }

        CompositeDisposable _CompositeDisposable;
        CancellationTokenSource _AnimationCts;
        private void TransientContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _CompositeDisposable = new CompositeDisposable();
            
           
            this.ObserveDependencyProperty(IsAutoHideEnabledProperty)
                .Subscribe(__ =>
                {
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                    {
                        ResetAnimation();
                    });
                })
                .AddTo(_CompositeDisposable);

            this.ObserveDependencyProperty(ContentProperty)
                .Subscribe(__ =>
                {
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ResetAnimation();
                    });
                })
                .AddTo(_CompositeDisposable);

           
        }

        object _AnimationCtsLock = new object();
        private FrameworkElement _container;

        private void TransientContainer_Unloaded(object sender, RoutedEventArgs e)
        {
            _AnimationCts?.Dispose();
            _AnimationCts = null;
            _CompositeDisposable?.Dispose();
        }

        void ResetAnimation()
        {
            lock (_AnimationCtsLock)
            {
                if (_AnimationCts != null)
                {
                    _AnimationCts.Cancel();
                    _AnimationCts.Dispose();
                }

                _AnimationCts = new CancellationTokenSource();
            }

            var ct = _AnimationCts.Token;

            if (Content != null)
            {
                if (IsAutoHideEnabled)
                {
                    var ab = AnimationBuilder.Create()
                        .Opacity(layer: FrameworkLayer.Xaml)
                        .TimedKeyFrames(b => b
                            .KeyFrame(FadeInDuration, 1.0, easingType: EasingType.Linear)
                            .KeyFrame(DisplayDuration + FadeInDuration, 1.0, easingType: EasingType.Linear)
                            .KeyFrame(DisplayDuration + FadeInDuration + FadeOutDuration, 0.0, easingType: EasingType.Linear)
                            );
                    ab.StartAsync(_container, ct);
                }
                else
                {
                    var ab = AnimationBuilder.Create()
                        .Opacity()
                        .TimedKeyFrames(b => b
                            .KeyFrame(FadeInDuration, 1.0, easingType: EasingType.Linear)
                            );
                    ab.StartAsync(_container, ct);
                }
            }
            else
            {
                var ab = AnimationBuilder.Create()
                        .Opacity()
                        .TimedKeyFrames(b => b
                            .KeyFrame(FadeOutDuration, 0.0, easingType: EasingType.Linear)
                            );
                ab.StartAsync(_container, ct);
            }
        }
    }
}

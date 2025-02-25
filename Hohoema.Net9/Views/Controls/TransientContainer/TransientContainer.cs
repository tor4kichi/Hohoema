﻿#nullable enable
using Microsoft.Toolkit.Uwp.UI.Animations;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Threading;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Hohoema.Views.Controls;

public sealed partial class TransientContainer : Control
{
    private readonly DispatcherQueue _dispactherQueue;

    public TransientContainer()
    {
        this.DefaultStyleKey = typeof(TransientContainer);

        Loaded += TransientContainer_Loaded;
        Unloaded += TransientContainer_Unloaded;

        _dispactherQueue = DispatcherQueue.GetForCurrentThread();
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

    CompositeDisposable? _CompositeDisposable;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP006:Implement IDisposable.", Justification = "<保留中>")]
    CancellationTokenSource? _AnimationCts;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don't ignore created IDisposable.", Justification = "<保留中>")]
    private void TransientContainer_Loaded(object sender, RoutedEventArgs e)
    {
        _CompositeDisposable = new CompositeDisposable();        
       
        this.ObserveDependencyProperty(IsAutoHideEnabledProperty)
            .Subscribe(__ =>
            {
                _dispactherQueue.TryEnqueue(() => 
                {
                    ResetAnimation();
                });
            })
            .AddTo(_CompositeDisposable);

        this.ObserveDependencyProperty(ContentProperty)
            .Subscribe(__ =>
            {
                _dispactherQueue.TryEnqueue(() =>
                {
                    ResetAnimation();
                });
            })
            .AddTo(_CompositeDisposable);

       
    }

    object _AnimationCtsLock = new object();
    private FrameworkElement? _container;

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


    public TimeSpan FadeInDuration
    {
        get { return (TimeSpan)GetValue(FadeInDurationProperty); }
        set { SetValue(FadeInDurationProperty, value); }
    }

    public static readonly DependencyProperty FadeInDurationProperty =
        DependencyProperty.Register("FadeInDuration", typeof(TimeSpan), typeof(TransientContainer), new PropertyMetadata(TimeSpan.FromSeconds(0.1)));



    public TimeSpan FadeOutDuration
    {
        get { return (TimeSpan)GetValue(FadeOutDurationProperty); }
        set { SetValue(FadeOutDurationProperty, value); }
    }

    public static readonly DependencyProperty FadeOutDurationProperty =
        DependencyProperty.Register("FadeOutDuration", typeof(TimeSpan), typeof(TransientContainer), new PropertyMetadata(TimeSpan.FromSeconds(0.1)));

}

﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Windows.UI.Xaml;

namespace Hohoema.Views.Extensions;

public sealed class DependencyObjectExtensions : DependencyObject
{
    public static readonly DependencyProperty FlyoutTemplateSelectorProperty =
        DependencyProperty.RegisterAttached(
            "DisposeOnUnloadedTarget",
            typeof(FrameworkElement),
            typeof(DependencyObjectExtensions),
            new PropertyMetadata(default(FrameworkElement), DisposeOnUnloadedTargetPropertyChanged)
        );

    public static void SetDisposeOnUnloadedTarget(DependencyObject element, FrameworkElement value)
    {
        element.SetValue(FlyoutTemplateSelectorProperty, value);
    }
    public static FrameworkElement GetDisposeOnUnloadedTarget(DependencyObject element)
    {
        return (FrameworkElement)element.GetValue(FlyoutTemplateSelectorProperty);
    }

    static Dictionary<FrameworkElement, CompositeDisposable> _disposersMap = new Dictionary<FrameworkElement, CompositeDisposable>();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created.", Justification = "<保留中>")]
    private static void DisposeOnUnloadedTargetPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is FrameworkElement newTarget)
        {
            var disposer = s as IDisposable;
            if (disposer == null)
            {
                throw new NotSupportedException($"DisposeOnUnloadedTarget must attached to FrameworkElement. now attach to {s?.ToString() ?? "null"}");
            }

            if (!_disposersMap.TryGetValue(newTarget, out var cd))
            {
                _disposersMap.Add(newTarget, cd = new CompositeDisposable());
                newTarget.Unloaded += NewTarget_Unloaded;
            }

            cd.Add(disposer);                
        }
        
        if (e.OldValue is FrameworkElement oldTarget)
        {
            oldTarget.Unloaded -= NewTarget_Unloaded;

            var disposer = s as IDisposable;
            if (disposer == null)
            {
                return;
            }

            if (_disposersMap.TryGetValue(oldTarget, out var cd))
            {
                cd.Remove(disposer);
                if (!cd.Any())
                {
                    _disposersMap.Remove(oldTarget);
                }
            }
            
        }
    }

    private static void NewTarget_Unloaded(object sender, RoutedEventArgs e)
    {
        var fe = sender as FrameworkElement;
        fe.Unloaded -= NewTarget_Unloaded;

        if (_disposersMap.Remove(fe, out var d))
        {
            d.Clear();
            d.Dispose();
        }
    }
}

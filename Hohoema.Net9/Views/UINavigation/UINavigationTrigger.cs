﻿#nullable enable
using Hohoema.Views.UINavigation;
using Microsoft.Xaml.Interactivity;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Hohoema.Views.Behaviors;

/// <summary>
/// UINavigationControllerの入力をBehavior(Trigger)として扱えるようにします。
/// Kindが設定されると定期検出処理が走るようになります。
/// </summary>
[ContentProperty(Name = "Actions")]
public sealed class UINavigationTrigger : Behavior<FrameworkElement>
{
    public UINavigationTrigger()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }


    #region Dependency Properties


    // フォーカスがある時だけ入力を処理するか
    public bool IsRequireFocus
    {
        get { return (bool)GetValue(IsRequireFocusProperty); }
        set { SetValue(IsRequireFocusProperty, value); }
    }

    public static readonly DependencyProperty IsRequireFocusProperty =
        DependencyProperty.Register(
            nameof(IsRequireFocus),
            typeof(bool),
            typeof(UINavigationTrigger),
            new PropertyMetadata(false)
            );



    public UINavigationButtons Kind
    {
        get { return (UINavigationButtons)GetValue(KindProperty); }
        set { SetValue(KindProperty, value); }
    }

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(
            nameof(Kind),
            typeof(UINavigationButtons), 
            typeof(UINavigationTrigger), 
            new PropertyMetadata(UINavigationButtons.None)
            );

    
    public bool Hold
    {
        get { return (bool)GetValue(HoldProperty); }
        set { SetValue(HoldProperty, value); }
    }

    public static readonly DependencyProperty HoldProperty =
        DependencyProperty.Register(
            nameof(Hold),
            typeof(bool),
            typeof(UINavigationTrigger),
            new PropertyMetadata(false)
            );

    public bool IsEnabled
    {
        get { return (bool)GetValue(IsEnabledProperty); }
        set { SetValue(IsEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(
            nameof(IsEnabled), 
            typeof(bool), 
            typeof(UINavigationTrigger), 
            new PropertyMetadata(true)
            );



    public ActionCollection Actions
    {
        get
        {
            if (GetValue(ActionsProperty) == null)
            {
                return this.Actions = new ActionCollection();
            }
            return (ActionCollection)GetValue(ActionsProperty);
        }
        set { SetValue(ActionsProperty, value); }
    }

    public static readonly DependencyProperty ActionsProperty =
        DependencyProperty.Register(
            nameof(Actions),
            typeof(ActionCollection),
            typeof(UINavigationTrigger),
            new PropertyMetadata(null));


    #endregion

    bool _NowFocusingElement = false;

    private readonly DispatcherQueue _dispatcherQueue;

    protected override void OnAttached()
    {
        this.AssociatedObject.GotFocus += AssociatedObject_GotFocus;
        this.AssociatedObject.LostFocus += AssociatedObject_LostFocus;

        UINavigationManager.Pressed += Instance_Pressed;
        UINavigationManager.Holding += Instance_Holding;
    }

    private void Instance_Holding(UINavigationManager sender, UINavigationButtons button)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (Hold && button.HasFlag(Kind))
            {
                foreach (var action in Actions.Cast<IAction>())
                {
                    action.Execute(this.AssociatedObject, null);
                }

                Debug.WriteLine($"Hold : {button}");
            }
        });
    }

    protected override void OnDetaching()
    {
        UINavigationManager.Pressed -= Instance_Pressed;
        UINavigationManager.Holding -= Instance_Holding;
        base.OnDetaching();
    }

    private void Instance_Pressed(UINavigationManager sender, UINavigationButtons button)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!IsEnabled) { return; }

            if (Hold)
            {
                return;
            }

            if (Windows.UI.ViewManagement.InputPane.GetForCurrentView().Visible)
            {
                return;
            }

            if (IsRequireFocus && !_NowFocusingElement)
            {
                return;
            }

            if (!button.HasFlag(Kind))
            {
                return;
            }
           
            foreach (var action in Actions.Cast<IAction>())
            {
                action.Execute(this.AssociatedObject, null);
            }
        });
    }

    private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
    {
        _NowFocusingElement = false;
    }

    private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
    {
        _NowFocusingElement = true;
    }
}

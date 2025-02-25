﻿#nullable enable
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
// this code is copy from  http://stackoverflow.com/questions/24066687/windows-phone-8-1-flyout-hide-with-behaviour-issue

namespace Hohoema.Views.Behaviors;

public class CloseFlyoutAction : DependencyObject, IAction
	{
		public object Execute(object sender, object parameter)
		{
			var element = sender as FrameworkElement;
			var flyout = element.FindFirstChild<FlyoutPresenter>();
			if (flyout == null) { return null; }
			var popup = flyout.Parent as Popup;
			if (popup != null)
			{
				popup.IsOpen = false;
			}
			return null;
		}
	}

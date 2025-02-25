﻿#nullable enable
using Hohoema.Models.Notification;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Contracts.Services;

public interface INotificationService
{
    void DismissInAppNotification();
    void HideToast();
    void ShowInAppNotification(InAppNotificationPayload payload);
    void ShowLiteInAppNotification(string content, DisplayDuration? displayDuration = null, Symbol? symbol = null);
    void ShowLiteInAppNotification(string content, TimeSpan duration, Symbol? symbol = null);
    void ShowLiteInAppNotification_Fail(string content, DisplayDuration? displayDuration = null);
    void ShowLiteInAppNotification_Fail(string content, TimeSpan duration);
    void ShowLiteInAppNotification_Success(string content, DisplayDuration? displayDuration = null);
    void ShowLiteInAppNotification_Success(string content, TimeSpan duration);
    void ShowToast(
       string title,
       string content,
       ToastDuration duration = ToastDuration.Short,
       bool isSuppress = false,
       string? luanchContent = null,
       Action? toastActivatedAction = null,
       IToastButton[]? toastButtons = null,
       IToastInput[]? toastInputs = null,
       ToastContextMenuItem[]? toastMenuItems = null
       );
}
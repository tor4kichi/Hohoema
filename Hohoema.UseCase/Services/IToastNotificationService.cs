using Microsoft.Toolkit.Uwp.Notifications;
using System;

namespace Hohoema.UseCase.Services
{
    public interface IToastNotificationService
    {
        void ShowToast(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false, string luanchContent = null, Action toastActivatedAction = null);
        void ShowToast(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false, string luanchContent = null, Action toastActivatedAction = null, params ToastButton[] toastButtons);
    }
}
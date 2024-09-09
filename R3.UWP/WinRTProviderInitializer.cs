using System;
using System.Windows;
using Windows.System;

namespace R3.UWP;

public static class WinRTProviderInitializer
{
    public static void SetDefaultObservableSystem(Action<Exception> unhandledExceptionHandler)
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(unhandledExceptionHandler);
        ObservableSystem.DefaultTimeProvider = new DispatcherQueueTimerProvider();
        ObservableSystem.DefaultFrameProvider = new TimerFrameProvider(TimeSpan.Zero, TimeSpan.FromMilliseconds(32), ObservableSystem.DefaultTimeProvider);
    }

    public static void SetDefaultObservableSystem(Action<Exception> unhandledExceptionHandler, DispatcherQueue dispatcher)
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(unhandledExceptionHandler);
        ObservableSystem.DefaultTimeProvider = new DispatcherQueueTimerProvider(dispatcher);
        ObservableSystem.DefaultFrameProvider = new TimerFrameProvider(TimeSpan.Zero, TimeSpan.FromMilliseconds(32), ObservableSystem.DefaultTimeProvider);
    }
}
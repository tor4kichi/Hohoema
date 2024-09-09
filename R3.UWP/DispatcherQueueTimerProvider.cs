using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;

#nullable enable
namespace R3.UWP;

public sealed class DispatcherQueueTimerProvider : TimeProvider
{
    readonly DispatcherQueue? dispatcher;

    public DispatcherQueueTimerProvider()
    {
        this.dispatcher = null;
    }

    public DispatcherQueueTimerProvider(DispatcherQueue dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return new DispatcherQueueTimerProviderTimer(dispatcher, callback, state, dueTime, period);
    }
}

internal sealed class DispatcherQueueTimerProviderTimer : ITimer
{
    DispatcherQueueTimer? timer;
    TimerCallback callback;
    object? state;
    TypedEventHandler<DispatcherQueueTimer, object> timerTick;
    TimeSpan? period;

    public DispatcherQueueTimerProviderTimer(DispatcherQueue? dispatcher, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        this.timerTick = Timer_Tick;
        this.callback = callback;
        this.state = state;

        if (dispatcher == null) // priority is not null
        {
            this.timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        }
        else
        {
            this.timer = dispatcher.CreateTimer();
        }

        timer.Tick += timerTick;

        if (dueTime != Timeout.InfiniteTimeSpan)
        {
            Change(dueTime, period);
        }
    }

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        if (timer != null)
        {
            this.period = period;
            timer.Interval = dueTime;

            timer.Start();
            return true;
        }
        return false;
    }

    void Timer_Tick(object sender, object e)
    {
        callback(state);

        if (timer != null && period != null)
        {
            if (period.Value == Timeout.InfiniteTimeSpan)
            {
                period = null;
                timer.Stop();
            }
            else
            {
                timer.Interval = period.Value;
                period = null;
                timer.Start();
            }
        }
    }

    public void Dispose()
    {
        if (timer != null)
        {
            timer.Stop();
            timer.Tick -= timerTick;
            timer = null;
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}

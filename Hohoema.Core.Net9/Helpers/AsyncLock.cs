﻿#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Helpers;

// this code copied from :
// https://github.com/Microsoft-Build-2016/CodeLabs-UWP/blob/master/Workshop/Module3-ConnectedApps/Source/End/Microsoft.Labs.SightsToSee/Microsoft.Labs.SightsToSee.Library/Utilities/AsyncLock.cs

public sealed class AsyncLock
{
    private readonly SemaphoreSlim m_semaphore = new(1, 1);
    private readonly Task<IDisposable> m_releaser;

    public AsyncLock()
    {
        m_releaser = Task.FromResult((IDisposable)new Releaser(this));
    }

    public Task<IDisposable> LockAsync(CancellationToken ct = default)
    {
        Task wait = m_semaphore.WaitAsync(ct);
        return wait.IsCompleted ?
                    m_releaser :
                    wait.ContinueWith((_, state) => (IDisposable)state,
                        m_releaser.Result, ct,
        TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly AsyncLock m_toRelease;
        internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }
        public void Dispose() { _ = m_toRelease.m_semaphore.Release(); }
    }
}

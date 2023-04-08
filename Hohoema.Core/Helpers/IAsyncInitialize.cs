using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Helpers;

public interface IAsyncInitialize
{
    bool IsInitialized { get; }

    Task Initialize();

    void Cancel();
}


public abstract class AsyncInitialize : ObservableObject, IAsyncInitialize
{
    public bool IsInitialized { get; private set; } = false;

    private readonly AsyncLock _InitializeLock = new();

    public bool IsCancel { get; private set; }

    private CancellationTokenSource _CancellationTokenSource;

    public async Task Initialize()
    {
        using IDisposable releaser = await _InitializeLock.LockAsync();
        if (IsInitialized) { return; }

        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _CancellationTokenSource = new CancellationTokenSource();

        try
        {
            CancellationToken ct = _CancellationTokenSource.Token;

            await OnInitializeAsync(ct);

            IsInitialized = true;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Initialize Canceled");
        }
        finally
        {
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = null;
        }
    }


    public void Cancel()
    {
        _CancellationTokenSource?.Cancel();

        Debug.WriteLine("Initialize Cancel Requested");
    }

    protected abstract Task OnInitializeAsync(CancellationToken token);

}

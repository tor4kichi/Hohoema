﻿#nullable enable
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Threading;

namespace Hohoema.ViewModels;

public abstract class HohoemaPageViewModelBase : NavigationAwareViewModelBase, IDisposable
	{
    public HohoemaPageViewModelBase()
    {
        _CompositeDisposable = new CompositeDisposable();
    }
    
    protected CompositeDisposable _CompositeDisposable { get; }
    protected CompositeDisposable? _navigationDisposables { get; private set; }

    private CancellationTokenSource? _navigationCts;

    protected CancellationToken NavigationCancellationToken { get; private set; }

    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }


    public virtual void Dispose()
    {
        _CompositeDisposable?.Dispose();
    }

    public override void OnNavigatingTo(INavigationParameters parameters) 
    {
        _navigationDisposables?.Dispose();
        _navigationDisposables = new();
        _navigationCts = new CancellationTokenSource()
            .AddTo(_navigationDisposables);
        NavigationCancellationToken = _navigationCts.Token;
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        if (_navigationCts is not null)
        {
            _navigationCts.Cancel();
            _navigationCts.Dispose();
            _navigationCts = null;
        }
        
        if (_navigationDisposables is not null)
        {
            _navigationDisposables.Dispose();
        }
        _navigationDisposables = new();
    }

}

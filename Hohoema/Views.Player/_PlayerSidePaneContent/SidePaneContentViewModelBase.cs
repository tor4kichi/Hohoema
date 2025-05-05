#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

public abstract class SidePaneContentViewModelBase : ObservableObject, IDisposable
{
	protected readonly CompositeDisposable _CompositeDisposable;

	public SidePaneContentViewModelBase()
	{
		_CompositeDisposable = new CompositeDisposable();
    }

	virtual public Task OnEnter() { return Task.CompletedTask; }
	virtual public void OnLeave() { }

	public virtual void Dispose()
	{
        _CompositeDisposable?.Dispose();
	}
}

using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Contracts.Navigations;
using Hohoema.Contracts.Navigations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels;

public abstract class NavigationAwareViewModelBase : ObservableObject, INavigationAware
{
    public virtual void OnNavigatedFrom(INavigationParameters parameters)
    {

    }

    public virtual void OnNavigatingTo(INavigationParameters parameters)
    {

    }

    public virtual void OnNavigatedTo(INavigationParameters parameters)
    {

    }

    public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        return Task.CompletedTask;
    }
}
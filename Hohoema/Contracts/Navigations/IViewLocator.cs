using System;

namespace Hohoema.Contracts.Services.Navigations;

public interface IViewLocator
{
    Type ResolveView(string viewName);
}

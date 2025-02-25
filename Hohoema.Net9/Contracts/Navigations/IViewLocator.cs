#nullable enable
using System;

namespace Hohoema.Contracts.Navigations;

public interface IViewLocator
{
    Type ResolveView(string viewName);
}

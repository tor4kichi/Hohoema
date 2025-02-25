using System;

namespace Hohoema.Contracts.Navigations;

public interface INavigationResult
{
    bool IsSuccess { get; }
    Exception Exception { get; }
}


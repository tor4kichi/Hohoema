#nullable enable
using System;

namespace Hohoema.Models.PageNavigation;

public interface ITitleUpdatablePage
{
    IObservable<string> GetTitleObservable();
}

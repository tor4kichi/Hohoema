﻿#nullable enable
using Windows.UI;
using Windows.UI.Xaml;

namespace Hohoema.Services;

public sealed class CurrentActiveWindowUIContextService
{
    public UIContext UIContext { get; private set; }

    public XamlRoot XamlRoot { get; private set; }

    public static void SetUIContext(CurrentActiveWindowUIContextService service, UIContext uIContext, XamlRoot xamlRoot)
    {
        service.UIContext = uIContext;
        service.XamlRoot = xamlRoot;
    }
}
